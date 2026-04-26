using UnityEngine;
using System.Collections.Generic;

public class BoardGeneratorv2 : MonoBehaviour
{
    [Header("Generator Settings")]
    public int initialFilledRows = 3;
    public int emptyRows = 9;

    [Header("References")]
    public Cellv2 cellPrefabv2;
    public GridManager gridManager;

    void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("Chưa kéo GridManager vào inspector của BoardGeneratorv2!");
            return;
        }

        gridManager.rows = initialFilledRows + emptyRows;
        GenerateGrid();
    }

    private int GetTargetPairs(int stage)
    {
        if (stage == 1) return 3;
        if (stage == 2) return 2;
        return 1;
    }

    public void GenerateGrid()
    {
        gridManager.InitializeBoard();

        int columns = gridManager.columns;
        float spacing = gridManager.spacing;
        int totalRows = gridManager.rows;
        int initialFilledCount = columns * initialFilledRows;

        // Tạo mảng ngẫu nhiên hoàn toàn (không còn cân bằng 1-9)
        int[] numberPool = RandomArray(initialFilledCount);

        // Lấy stage hiện tại
        int currentStage = GameManager.Instance != null ? GameManager.Instance.currentStage : 1;
        int targetPairs = GetTargetPairs(currentStage);

        // Sinh board với đúng số cặp yêu cầu bằng logic mới
        numberPool = GenerateBoardWithExactPairs(numberPool, targetPairs, columns);

        // Tạo grid
        for (int y = 0; y < totalRows; y++)
        {
            Transform rowContainer = new GameObject($"Row_{y}").transform;
            rowContainer.SetParent(gridManager.gridContainer);
            rowContainer.localPosition = new Vector3(0, -y * spacing, 0);

            for (int x = 0; x < columns; x++)
            {
                Cellv2 newCell = Instantiate(cellPrefabv2, rowContainer);
                newCell.transform.localPosition = new Vector3(x * spacing, 0, 0);

                int index = y * columns + x;
                int cellValue = (index < initialFilledCount) ? numberPool[index] : 0;

                newCell.Setup(x, y, cellValue);
                gridManager.AddCell(index, newCell);
            }
        }

        gridManager.SetupBoardBackground();
        gridManager.CenterGrid();
    }

    // ==================== TẠO MẢNG NGẪU NHIÊN 1-9 ====================
    private int[] RandomArray(int length)
    {
        int[] arr = new int[length];
        for (int i = 0; i < length; i++)
        {
            arr[i] = Random.Range(1, 10); // Ngẫu nhiên 1 đến 9
        }
        return arr;
    }

    // ─── BƯỚC 1: TÍNH DANH SÁCH Ô LÂN CẬN (CHỈ NGANG, DỌC, NỐI ĐUÔI) ───
    private int[][] _neighbors;

    private int[][] GetNeighbors(int rows, int columns)
    {
        if (_neighbors != null) return _neighbors;
        int size = rows * columns;
        _neighbors = new int[size][];
        
        for (int i = 0; i < size; i++)
        {
            List<int> neighbors = new List<int>();
            int r = i / columns;
            int c = i % columns;

            // Duyệt 8 hướng kề và chéo
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue; // Bỏ qua chính nó
                    
                    int nr = r + dr;
                    int nc = c + dc;
                    
                    // Chỉ lấy nếu nằm trong biên của ma trận, KHÔNG wrap-around
                    if (nr >= 0 && nr < rows && nc >= 0 && nc < columns)
                    {
                        neighbors.Add(nr * columns + nc);
                    }
                }
            }

            // Bổ sung luật nối đuôi (wrap-around) ngang:
            // Ô cuối cùng của hàng này kề với ô đầu tiên của hàng kế tiếp
            if (c == columns - 1 && i + 1 < size)
            {
                neighbors.Add(i + 1);
            }
            // Khứ hồi: Ô đầu tiên của hàng này kề với ô cuối cùng của hàng trước
            if (c == 0 && i - 1 >= 0)
            {
                neighbors.Add(i - 1);
            }

            _neighbors[i] = neighbors.ToArray();
        }
        return _neighbors;
    }

    // ─── BƯỚC 2: HÀM KIỂM TRA VI PHẠM ───
    private bool IsViolation(int a, int b)
    {
        return a == b || a + b == 10;
    }

    private int CountViolations(int[] arr, int[][] neighbors)
    {
        int count = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            foreach (int j in neighbors[i])
            {
                // Chỉ xét j > i để không đếm trùng lặp
                if (j > i && IsViolation(arr[i], arr[j]))
                {
                    count++;
                }
            }
        }
        return count;
    }

    private List<(int, int)> GetViolationPairs(int[] arr, int[][] neighbors)
    {
        List<(int, int)> pairs = new List<(int, int)>();
        for (int i = 0; i < arr.Length; i++)
        {
            foreach (int j in neighbors[i])
            {
                if (j > i && IsViolation(arr[i], arr[j]))
                {
                    pairs.Add((i, j));
                }
            }
        }
        return pairs;
    }

    // ─── BƯỚC 4: THUẬT TOÁN HILL-CLIMBING CHÍNH (CÓ RANDOM RESTART) ───
    private int[] GenerateBoardWithExactPairs(int[] pool, int targetPairs, int columns)
    {
        int rows = pool.Length / columns;
        int[][] neighbors = GetNeighbors(rows, columns);
        
        int[] arr = (int[])pool.Clone();
        
        int violations = CountViolations(arr, neighbors);
        int iter = 0;
        int maxIter = 200000;
        int restartEvery = 5000;

        while (violations != targetPairs && iter < maxIter)
        {
            iter++;

            List<(int, int)> pairs = GetViolationPairs(arr, neighbors);
            
            int target;
            if (pairs.Count == 0) 
            {
                // Nếu hiện tại = 0 cặp, ép buộc chọn 1 ô ngẫu nhiên để phá
                target = Random.Range(0, arr.Length);
            }
            else
            {
                // Chọn ngẫu nhiên 1 cặp vi phạm để cố gắng sửa
                var p = pairs[Random.Range(0, pairs.Count)];
                target = Random.value < 0.5f ? p.Item1 : p.Item2;
            }

            int oldValue = arr[target];
            int[] bestArr = (int[])arr.Clone();
            int bestDiff = Mathf.Abs(violations - targetPairs);

            // Thử tất cả giá trị 1-9 cho ô target
            for (int v = 1; v <= 9; v++)
            {
                if (v == oldValue) continue; // Bỏ qua giá trị cũ
                
                arr[target] = v; // Gán thử giá trị mới
                
                int nv = CountViolations(arr, neighbors);
                int diff = Mathf.Abs(nv - targetPairs);
                
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestArr = (int[])arr.Clone();
                }
            }

            // Cập nhật trạng thái tốt nhất
            arr = bestArr;
            violations = CountViolations(arr, neighbors);

            if (violations == targetPairs) break;

            // --- Random Restart: Tránh kẹt ở đỉnh cục bộ ---
            if (iter % restartEvery == 0)
            {
                arr = RandomArray(arr.Length);
                violations = CountViolations(arr, neighbors);
            }
        }

        if (violations != targetPairs)
        {
            Debug.LogWarning($"Không thể đạt chính xác {targetPairs} cặp sau {maxIter} vòng lặp. Hiện tại: {violations}");
        }
        else
        {
            Debug.Log($"[Hill-Climbing] Đạt {violations} cặp thành công sau {iter} vòng lặp.");
        }

        return arr;
    }

    // ─── SHUFFLE MẢNG (FISHER-YATES) ───
    private void ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}