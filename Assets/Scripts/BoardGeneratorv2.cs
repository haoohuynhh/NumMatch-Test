using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardGeneratorv2 : MonoBehaviour
{
    [Header("Generator Settings")]
    public int initialFilledRows = 3;
    public int emptyRows = 9;

    [Header("References")]
    public Cellv2 cellPrefabv2;
    public GridManager gridManager;

    // System.Random dùng cho background thread (UnityEngine.Random chỉ dùng trên main thread)
    private System.Random _rng;

    // ==================== UNITY LIFECYCLE ====================

    void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("Chưa kéo GridManager vào inspector của BoardGeneratorv2!");
            return;
        }

        // gridManager.rows được set bên trong coroutine,
        // ngay trước InitializeBoard() để tránh bị override trong lúc yield
        GenerateGrid();
    }

    // ==================== PUBLIC API ====================

    public void GenerateGrid()
    {
        StartCoroutine(GenerateGridAsync());
    }

    // ==================== COROUTINE CHÍNH ====================

    private IEnumerator GenerateGridAsync()
    {
        // Chỉ đọc những giá trị KHÔNG bị InitializeBoard() thay đổi
        int currentStage = GameManager.Instance != null ? GameManager.Instance.currentStage : 1;
        int targetPairs  = GetTargetPairs(currentStage);

        // columns cần để chạy thuật toán trên thread — đọc trước yield
        int columns     = gridManager.columns;
        int filledCount = columns * initialFilledRows;

        // Tạo pool & rng trên main thread
        int[] numberPool = CreateBalancedPool(filledCount);
        _rng = new System.Random();

        // Chạy thuật toán nặng trên background thread
        int[] result = null;
        bool done    = false;

        var thread = new System.Threading.Thread(() =>
        {
            result = GenerateBoardWithExactPairs(numberPool, targetPairs, columns);
            done   = true;
        });
        thread.Start();

        // Nhường frame, không freeze Unity
        while (!done)
            yield return null;

        // ── Từ đây trở xuống: tất cả chạy liền nhau trong 1 frame, không yield ──

        // Set rows trước, rồi InitializeBoard() đọc giá trị đó
        gridManager.rows = initialFilledRows + emptyRows;
        gridManager.InitializeBoard();

        // Đọc spacing/totalRows SAU InitializeBoard() để lấy giá trị chính xác
        float spacing  = gridManager.spacing;
        int totalRows  = gridManager.rows;

        // Dựng grid ngay lập tức trong cùng frame
        BuildGrid(result, columns, spacing, totalRows, filledCount);
    }

    // ==================== DỰNG GRID (MAIN THREAD) ====================

    private void BuildGrid(int[] numberPool, int columns, float spacing, int totalRows, int initialFilledCount)
    {
        List<Cellv2> initialCells = new List<Cellv2>();

        for (int y = 0; y < totalRows; y++)
        {
            Transform rowContainer = new GameObject($"Row_{y}").transform;
            rowContainer.SetParent(gridManager.gridContainer);
            rowContainer.localPosition = new Vector3(0, -y * spacing, 0);

            for (int x = 0; x < columns; x++)
            {
                Cellv2 newCell = Instantiate(cellPrefabv2, rowContainer);
                newCell.transform.localPosition = new Vector3(x * spacing, 0, 0);

                int index     = y * columns + x;
                int cellValue = (index < initialFilledCount) ? numberPool[index] : 0;

                newCell.Setup(x, y, cellValue);
                gridManager.AddCell(index, newCell);

                if (cellValue != 0)
                    initialCells.Add(newCell);
            }
        }

        gridManager.SetupBoardBackground();
        gridManager.CenterGrid();

        if (GameManager.Instance != null && initialCells.Count > 0)
            GameManager.Instance.SpawnGemsOnCells(initialCells, true);
    }

    // ==================== STAGE CONFIG ====================

    private int GetTargetPairs(int stage)
    {
        if (stage == 1) return 3;
        if (stage == 2) return 2;
        return 1;
    }

    // ==================== TẠO POOL SỐ CÂN BẰNG 1-9 ====================

    /// <summary>
    /// Tạo pool có mỗi số 1-9 xuất hiện đều nhau rồi shuffle.
    /// Dùng UnityEngine.Random vì chạy trên main thread.
    /// </summary>
    private int[] CreateBalancedPool(int length)
    {
        int[] arr      = new int[length];
        int baseCount  = length / 9;
        int remainder  = length % 9;

        int idx = 0;
        for (int v = 1; v <= 9; v++)
        {
            int count = baseCount + (v <= remainder ? 1 : 0);
            for (int i = 0; i < count; i++)
                arr[idx++] = v;
        }

        // Shuffle trên main thread → UnityEngine.Random OK
        ShuffleArrayMainThread(arr);
        return arr;
    }

 
    private void ShuffleArrayMainThread(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j    = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

 
    private void ShuffleArrayThreadSafe(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j    = _rng.Next(0, i + 1);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    // ==================== KIỂM TRA ĐƯỜNG ĐI HỢP LỆ ====================


    /// Cho phép 4 loại đường đi:
    ///   1. Ngang:     cùng hàng, không có ô nào chặn giữa.
    ///   2. Dọc:       cùng cột, không có ô nào chặn giữa.
    ///   3. Chéo 45°:  |dx| == |dy|, không có ô nào chặn trên đường chéo.
    ///   4. Nối đuôi:  ô cuối hàng Y liền kề ô đầu hàng Y+1 (đúng 1 bước wrap).

    private bool HasClearPath(int[] arr, int columns, int idx1, int idx2)
    {
        int x1 = idx1 % columns, y1 = idx1 / columns;
        int x2 = idx2 % columns, y2 = idx2 / columns;

        int dx = x2 - x1;
        int dy = y2 - y1;

        // ── Ngang: cùng hàng ──
        if (dy == 0)
        {
            int left  = Mathf.Min(x1, x2) + 1;
            int right = Mathf.Max(x1, x2);
            for (int x = left; x < right; x++)
                if (arr[y1 * columns + x] != 0) return false;
            return true;
        }

        // ── Dọc: cùng cột ──
        if (dx == 0)
        {
            int top = Mathf.Min(y1, y2) + 1;
            int bot = Mathf.Max(y1, y2);
            for (int y = top; y < bot; y++)
                if (arr[y * columns + x1] != 0) return false;
            return true;
        }

        // ── Chéo 45°: |dx| == |dy| ──
        if (Mathf.Abs(dx) == Mathf.Abs(dy))
        {
            int stepX = dx > 0 ? 1 : -1;
            int stepY = dy > 0 ? 1 : -1;
            int cx = x1 + stepX;
            int cy = y1 + stepY;

            // Duyệt các ô trung gian trên đường chéo (không bao gồm 2 đầu)
            while (cx != x2 || cy != y2)
            {
                if (arr[cy * columns + cx] != 0) return false;
                cx += stepX;
                cy += stepY;
            }
            return true;
        }

        // ── Nối đuôi: cuối hàng Y → đầu hàng Y+1, đúng 1 bước ──
        int minIdx = Mathf.Min(idx1, idx2);
        int maxIdx = Mathf.Max(idx1, idx2);
        if (minIdx % columns == columns - 1 && maxIdx % columns == 0 && maxIdx == minIdx + 1)
            return true;

        // Mọi trường hợp còn lại → không hợp lệ
        return false;
    }

    // ==================== KIỂM TRA CẶP HỢP LỆ ====================

    private bool IsMatchCandidate(int a, int b)
    {
        return a == b || a + b == 10;
    }

    private List<(int, int)> GetValidPairs(int[] arr, int columns)
    {
        var pairs = new List<(int, int)>();
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == 0) continue;
            for (int j = i + 1; j < arr.Length; j++)
            {
                if (arr[j] == 0) continue;
                if (!IsMatchCandidate(arr[i], arr[j])) continue;
                if (HasClearPath(arr, columns, i, j))
                    pairs.Add((i, j));
            }
        }
        return pairs;
    }

    // ==================== ĐẾM CẶP (MAXIMUM BIPARTITE MATCHING) ====================


    private int CountMaxMatching(int[] arr, int columns)
    {
        var pairs = GetValidPairs(arr, columns);
        if (pairs.Count == 0) return 0;

        // Xây adjacency list: left node → danh sách right node
        var adj = new Dictionary<int, List<int>>();
        foreach (var (a, b) in pairs)
        {
            if (!adj.ContainsKey(a)) adj[a] = new List<int>();
            adj[a].Add(b);
        }

        var matchL = new Dictionary<int, int>(); // left  → right
        var matchR = new Dictionary<int, int>(); // right → left
        int result = 0;

        foreach (int u in adj.Keys)
        {
            var visited = new HashSet<int>();
            if (TryAugment(u, adj, matchL, matchR, visited))
                result++;
        }
        return result;
    }

    private bool TryAugment(
        int u,
        Dictionary<int, List<int>> adj,
        Dictionary<int, int> matchL,
        Dictionary<int, int> matchR,
        HashSet<int> visited)
    {
        if (!adj.ContainsKey(u)) return false;
        foreach (int v in adj[u])
        {
            if (visited.Contains(v)) continue;
            visited.Add(v);
            if (!matchR.ContainsKey(v) || TryAugment(matchR[v], adj, matchL, matchR, visited))
            {
                matchL[u] = v;
                matchR[v] = u;
                return true;
            }
        }
        return false;
    }



    /// Tìm arrangement có đúng targetPairs cặp hợp lệ.
    /// Dùng _rng (System.Random) vì chạy trên background thread.

    private int[] GenerateBoardWithExactPairs(int[] pool, int targetPairs, int columns)
    {
        int[] best    = (int[])pool.Clone();
        int bestDiff  = int.MaxValue;

        int maxRestarts      = 20;
        int itersPerRestart  = 2000;
        int totalIterations  = 0;

        for (int restart = 0; restart < maxRestarts && bestDiff != 0; restart++)
        {
            int[] arr = (int[])pool.Clone();
            ShuffleArrayThreadSafe(arr); // ← System.Random, thread-safe

            for (int iter = 0; iter < itersPerRestart; iter++)
            {
                totalIterations++;
                
                int pairs = CountMaxMatching(arr, columns);
                int diff  = System.Math.Abs(pairs - targetPairs);

                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best     = (int[])arr.Clone();
                }
                
                if (diff == 0)
                {
                    Debug.Log($"[BoardGenerator] Đạt targetPairs={targetPairs} thành công sau {totalIterations} lần chạy (Restart: {restart}, Iter: {iter}).");
                    return arr; // Đạt đúng target → dừng sớm
                }

                // Chọn focus có hướng dẫn
                var validPairs = GetValidPairs(arr, columns);
                int focus;

                if (pairs > targetPairs && validPairs.Count > 0)
                {
                    // Quá nhiều cặp → chọn ngẫu nhiên một ô trong danh sách cặp hợp lệ
                    var chosen = validPairs[_rng.Next(0, validPairs.Count)];
                    focus = (_rng.NextDouble() < 0.5) ? chosen.Item1 : chosen.Item2;
                }
                else
                {
                    // Quá ít cặp → chọn random để thám hiểm
                    focus = _rng.Next(0, arr.Length);
                }

                // Thử 50 swap, giữ lại swap tốt nhất
                int[] candidate     = (int[])arr.Clone();
                int candidatePairs  = pairs;

                for (int t = 0; t < 50; t++)
                {
                    int swapIdx = _rng.Next(0, arr.Length);
                    if (swapIdx == focus) continue;

                    Swap(arr, focus, swapIdx);
                    int np = CountMaxMatching(arr, columns);

                    if (System.Math.Abs(np - targetPairs) < System.Math.Abs(candidatePairs - targetPairs))
                    {
                        candidate      = (int[])arr.Clone();
                        candidatePairs = np;
                    }

                    Swap(arr, focus, swapIdx); // Hoàn tác
                }

                arr = candidate;
            }
        }

        if (bestDiff != 0)
            Debug.LogWarning($"[BoardGenerator] Không đạt targetPairs={targetPairs}, bestDiff={bestDiff} sau tổng cộng {totalIterations} lần chạy.");
        else
            Debug.Log($"[BoardGenerator] Đạt targetPairs={targetPairs} thành công sau tổng cộng {totalIterations} lần chạy.");

        return best;
    }


    private void Swap(int[] arr, int a, int b)
    {
        int tmp = arr[a];
        arr[a]  = arr[b];
        arr[b]  = tmp;
    }
}