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

        // Tạo pool số cân bằng (đủ 1-9 và phân bố đều)
        int[] numberPool = CreateBalancedNumberPool(initialFilledCount);

        // Lấy stage hiện tại
        int currentStage = GameManager.Instance != null ? GameManager.Instance.currentStage : 1;
        int targetPairs = GetTargetPairs(currentStage);

        // Sinh board với đúng số cặp yêu cầu
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

    // ==================== TẠO POOL SỐ CÂN BẰNG (1-9 đều) ====================
    private int[] CreateBalancedNumberPool(int total)
    {
        int[] pool = new int[total];
        int baseCount = total / 9;
        int remainder = total % 9;

        int[] digitCounts = new int[9];
        for (int i = 0; i < 9; i++)
            digitCounts[i] = baseCount;

        // Rải phần dư ngẫu nhiên
        int[] indices = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        ShuffleArray(indices);
        for (int i = 0; i < remainder; i++)
            digitCounts[indices[i]]++;

        // Đổ vào pool
        int poolIndex = 0;
        for (int digit = 0; digit < 9; digit++)
        {
            for (int count = 0; count < digitCounts[digit]; count++)
            {
                pool[poolIndex++] = digit + 1;
            }
        }

        return pool;
    }

    // ==================== SINH BOARD CÓ ĐÚNG SỐ CẶP ====================
    private int[] GenerateBoardWithExactPairs(int[] pool, int targetPairs, int columns)
    {
        int[] board = (int[])pool.Clone();
        int maxAttempts = 5000;
        int[] bestBoard = (int[])board.Clone();
        int bestDiff = int.MaxValue;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            ShuffleArray(board);

            int count = CountPairs(board, columns, out _);

            if (count == targetPairs)
                return board;

            int diff = Mathf.Abs(count - targetPairs);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestBoard = (int[])board.Clone();
            }

            // Nếu chỉ chênh 1 cặp thì thử tinh chỉnh
            if (diff == 1)
            {
                if (TryRefineToExactPairs(board, targetPairs, columns, 400))
                    return board;
            }
        }

        Debug.LogWarning($"Không tìm được chính xác {targetPairs} cặp. Sử dụng cấu hình gần nhất (chênh {bestDiff}).");
        return bestBoard;
    }

    // ==================== ĐẾM SỐ CẶP (ĐÃ XỬ LÝ ĐẦY ĐỦ CHÉO) ====================
    private int CountPairs(int[] board, int columns, out List<(int, int)> nonOverlappingPairs)
    {
        bool[] used = new bool[board.Length];
        nonOverlappingPairs = new List<(int, int)>();
        int matchCount = 0;

        for (int i = 0; i < board.Length; i++)
        {
            if (used[i] || board[i] == 0) continue;

            // Ưu tiên thứ tự: Ngang → Dọc → Chéo phải → Chéo trái
            if (TryMatch(i, i + 1, board, columns, used, nonOverlappingPairs, ref matchCount)) continue;
            if (TryMatch(i, i + columns, board, columns, used, nonOverlappingPairs, ref matchCount)) continue;
            if (TryMatch(i, i + columns + 1, board, columns, used, nonOverlappingPairs, ref matchCount)) continue;
            if (TryMatch(i, i + columns - 1, board, columns, used, nonOverlappingPairs, ref matchCount)) continue;
        }

        return matchCount;
    }

    private bool TryMatch(int a, int b, int[] board, int columns, bool[] used, List<(int, int)> pairs, ref int count)
    {
        if (b < 0 || b >= board.Length || used[a] || used[b]) return false;
        if (!IsValidDirection(a, b, columns)) return false;

        if (board[a] == board[b] || board[a] + board[b] == 10)
        {
            used[a] = true;
            used[b] = true;
            pairs.Add((a, b));
            count++;
            return true;
        }
        return false;
    }

    private bool IsValidDirection(int a, int b, int columns)
    {
        int diff = Mathf.Abs(a - b);
        return diff == 1 || diff == columns || diff == columns + 1 || diff == columns - 1;
    }

    // ==================== TINH CHỈNH NHẸ KHI CHÊNH 1 CẶP ====================
    private bool TryRefineToExactPairs(int[] board, int target, int columns, int maxRefine = 400)
    {
        for (int i = 0; i < maxRefine; i++)
        {
            int count = CountPairs(board, columns, out var pairs);

            if (count == target) return true;

            if (count < target)
            {
                // Thiếu cặp → swap ngẫu nhiên
                int idx1 = Random.Range(0, board.Length);
                int idx2 = Random.Range(0, board.Length);
                Swap(board, idx1, idx2);
            }
            else
            {
                // Dư cặp → ưu tiên phá một cặp đang có
                if (pairs.Count > 0)
                {
                    var p = pairs[Random.Range(0, pairs.Count)];
                    int idx1 = Random.value < 0.5f ? p.Item1 : p.Item2;
                    int idx2 = Random.Range(0, board.Length);
                    if (idx1 != idx2) Swap(board, idx1, idx2);
                }
                else
                {
                    Swap(board, Random.Range(0, board.Length), Random.Range(0, board.Length));
                }
            }
        }
        return false;
    }

    private void Swap(int[] arr, int a, int b)
    {
        if (a != b)
        {
            (arr[a], arr[b]) = (arr[b], arr[a]);
        }
    }

    // ==================== SHUFFLE MẢNG ====================
    private void ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}