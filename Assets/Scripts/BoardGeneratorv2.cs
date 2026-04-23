using UnityEngine;
using System.Collections.Generic;

public class BoardGeneratorv2 : MonoBehaviour
{
    [Header("Generator Settings")]
    public int initialFilledRows = 3;   
    public int emptyRows = 9;           
    public int guaranteedPairs = 3;     

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

        // Cập nhật tổng số dòng cho GridManager
        gridManager.rows = initialFilledRows + emptyRows;
        
        GenerateGrid();
    }

    void GenerateGrid()
    {
            gridManager.InitializeBoard();

    int columns = gridManager.columns;   
    int rows = gridManager.rows;         
    float spacing = gridManager.spacing;

    for (int y = 0; y < rows; y++)
    {
        Transform rowContainer = new GameObject($"Row_{y}").transform;
        rowContainer.SetParent(gridManager.gridContainer);
        rowContainer.localPosition = new Vector3(0, -y * spacing, 0);

        for (int x = 0; x < columns; x++)
        {
            Cellv2 newCell = Instantiate(cellPrefabv2, rowContainer);
            newCell.transform.localPosition = new Vector3(x * spacing, 0, 0);


            int cellValue = (x % 9) + 1;

            int index = y * columns + x;

            newCell.Setup(x, y, cellValue);
            gridManager.AddCell(index, newCell);
        }
    }

    gridManager.SetupBoardBackground();
    gridManager.CenterGrid();
        // gridManager.InitializeBoard();

        // int columns = gridManager.columns;
        // float spacing = gridManager.spacing;
        // int totalRows = gridManager.rows;

        // int initialFilledCount = columns * initialFilledRows;
        // int[] numberPool = CreateBalancedNumberPool(initialFilledCount);
        
        // // Tạo bảng sao cho đảm bảo số lượng cặp đúng với yêu cầu
        // numberPool = GenerateBoardWithExactPairs(numberPool, guaranteedPairs, columns);

        // // Duyệt từng dòng (Y)
        // for (int y = 0; y < totalRows; y++)
        // {
        //     Transform rowContainer = new GameObject($"Row_{y}").transform;
        //     rowContainer.SetParent(gridManager.gridContainer);
            
        //     // TÍNH VỊ TRÍ CHIỀU Y (TỪ TRÊN XUỐNG DƯỚI)
        //     rowContainer.localPosition = new Vector3(0, -y * spacing, 0);

        //     // Duyệt từng cột (X)
        //     for (int x = 0; x < columns; x++)
        //     {
        //         Cellv2 newCell = Instantiate(cellPrefabv2, rowContainer);
        //         newCell.transform.localPosition = new Vector3(x * spacing, 0, 0);
                
        //         int cellValue = 0; 

        //         int index = y * columns + x;

        //         if (index < initialFilledCount)
        //         {
        //             cellValue = numberPool[index];
        //         }

        //         newCell.Setup(x, y, cellValue);

        //         gridManager.AddCell(index, newCell);
        //     }
        // }
        
        // gridManager.SetupBoardBackground(); 
        // gridManager.CenterGrid();
    }

    //  THUẬT TOÁN PHÂN BỔ SỐ SỬ DỤNG MẢNG
    private int[] CreateBalancedNumberPool(int total)
    {
        int[] pool = new int[total]; // Khởi tạo mảng với kích thước cố định bằng total
        int baseCount = total / 9; // Số lượng xuất hiện mặc định
        int remainder = total % 9; // Lượng dư cần rải thêm

        // Mảng lưu số lần xuất hiện thực tế của các số (Index 0 = số 1, Index 8 = số 9)
        int[] digitCounts = new int[9];

        // Gán baseCount cho tất cả 9 chữ số
        for (int i = 0; i < 9; i++)
        {
            digitCounts[i] = baseCount;
        }

        // Rải phần dư ngẫu nhiên để tạo ra mảng như ví dụ [5, 4, 5, 5, 4, 5, 5, 4, 5]
        int[] availableIndices = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        ShuffleArray(availableIndices); 

        for (int i = 0; i < remainder; i++)
        {
            digitCounts[availableIndices[i]]++; // Cộng thêm 1 lượt cho số may mắn này
        }

        // Đỏ toàn bộ số lượng đã tính toán vào mảng Pool
        int poolIndex = 0; // Biến vị trí để chèn vào mảng pool
        for (int digitIndex = 0; digitIndex < 9; digitIndex++)
        {
            int currentDigit = digitIndex + 1; // Chữ số từ 1-9
            for (int count = 0; count < digitCounts[digitIndex]; count++)
            {
                pool[poolIndex] = currentDigit;
                poolIndex++;
            }
        }

        return pool;
    }

    // --- THUẬT TOÁN XÁO TRỘN CHO MẢNG (Fisher-Yates Shuffle) ---
    
    private void ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }

    // --- THUẬT TOÁN ĐẢM BẢO CHÍNH XÁC SỐ CẶP (ĐÃ TỐI ƯU CHO DỄ HIỂU) ---
    private int[] GenerateBoardWithExactPairs(int[] pool, int targetPairs, int columns)
    {
        int[] currentBoard = (int[])pool.Clone();
        int maxIterations = 10000; // Số vòng lặp tối đa để tránh treo game
        int iterations = 0;
        
        // Cứ xáo trộn ngẫu nhiên liên tục cho đến khi nào mảng tạo ra có ĐÚNG số cặp yêu cầu
        while (iterations < maxIterations)
        {
            ShuffleArray(currentBoard);
            
            // Nếu đếm số cặp bằng đúng targetPairs thì dừng và lấy luôn kết quả này
            if (CountPairs(currentBoard, columns) == targetPairs)
            {
                return currentBoard;
            }
            
            iterations++;
        }
        
        Debug.LogWarning("Không thể tạo board với đúng " + targetPairs + " cặp. Trả về kết quả gần nhất.");
        return currentBoard;
    }

    // Đếm số lượng cặp hợp lệ theo cách đơn giản (từ trái qua phải, trên xuống dưới)
    // Đảm bảo: Nếu một số đã được ăn (match) thì không thể dùng để ăn số khác.
    private int CountPairs(int[] board, int columns)
    {
        int matchCount = 0;
        bool[] used = new bool[board.Length]; // Mảng đánh dấu các ô đã bị ăn (match)

        for (int i = 0; i < board.Length; i++)
        {
            // Nếu ô này đã bị ghép với ô khác rồi thì bỏ qua luôn
            if (used[i]) continue; 

            // 1. Thử ghép ngang với ô ngay bên phải (i + 1)
            // Lấy i + 1 vì trong mảng 1 chiều, ô kế tiếp chính là ô bên phải
            if (i + 1 < board.Length && !used[i + 1])
            {
                if (board[i] == board[i + 1] || board[i] + board[i + 1] == 10)
                {
                    used[i] = true;         // Đánh dấu ô hiện tại đã ăn
                    used[i + 1] = true;     // Đánh dấu ô bên phải đã ăn
                    matchCount++;
                    continue;               // Đã ghép ngang thành công thì chuyển sang ô tiếp theo, KHÔNG thử ghép dọc nữa
                }
            }

            // 2. Thử ghép dọc với ô ngay bên dưới (i + columns)
            if (i + columns < board.Length && !used[i + columns])
            {
                if (board[i] == board[i + columns] || board[i] + board[i + columns] == 10)
                {
                    used[i] = true;         // Đánh dấu ô hiện tại đã ăn
                    used[i + columns] = true; // Đánh dấu ô bên dưới đã ăn
                    matchCount++;
                }
            }
        }
        
        return matchCount;
    }
}
