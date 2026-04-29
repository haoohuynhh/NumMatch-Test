using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int columns = 9;       
    public int rows = 12;          
    public float spacing = 1.0f;  

    [Header("References")]
    public Transform gridContainer; 
    public Transform boardBackground; 
    public float padding = 0.5f;
    public BoardGeneratorv2 boardGenerator; // Dùng để lấy cellPrefab, kéo BoardGeneratorv2 vào

    [Header("VFX")]
    public MatchLineSpawner matchLineSpawner;

    // Quản lý mảng 1 chiều chứa các ô
    public Cellv2[] board;
    public int addNumber = 6;

    private bool _isClearingRows;

    public void Start()
    {
        
        addNumber = 6; // Reset lượt chọn khi bắt đầu

    
    }


    public void InitializeBoard()
    {
        board = new Cellv2[columns * rows];
    }

    public void AddCell(int index, Cellv2 cell)
    {
        if (board != null && index >= 0 && index < board.Length)
        {
            board[index] = cell;
        }
    }

    public Cellv2 GetCell(int x, int y)
    {
        if (x >= 0 && x < columns && y >= 0 && y < rows)
        {
            return board[y * columns + x];
        }
        return null;
    }

    public void SetupBoardBackground()
    {
        if (boardBackground != null)
        {
            // Tính toán kích thước thật của bảng
            float width = (columns - 1) * spacing + padding * 2;
            float height = (rows - 1) * spacing + padding * 2;
            
            // Ép tấm nền scale theo kích thước vừa tính
            boardBackground.localScale = new Vector3(width, height, 1);
            
            // Đẩy tấm nền vào đúng tâm của Grid
            // Chiều Y đi xuống nên nhân với âm
            boardBackground.localPosition = new Vector3(
                (columns - 1) * spacing / 2f, 
                -(rows - 1) * spacing / 2f, 
                0
            );
        }
    }

    public void CenterGrid()
    {
        if (gridContainer != null)
        {
            float gridWidth = (columns - 1) * spacing;
            float gridHeight = (rows - 1) * spacing;
            
            // Dùng localPosition để không phụ thuộc vào vị trí của Parent
            gridContainer.localPosition = new Vector3(-gridWidth / 2f, gridHeight / 2f, 0);

            // Sau khi căn giữa xong, yêu cầu DragScroll cập nhật lại Bounds
            DragScroll ds = gridContainer.GetComponent<DragScroll>();
            if (ds == null) ds = FindObjectOfType<DragScroll>();
            if (ds != null) ds.ResetScroll();
        }
    }

    public void CheckAndClearMatchedRows()
    {
        if (_isClearingRows) return;
        StartCoroutine(ClearRowsWithDelay());
    }

    private IEnumerator ClearRowsWithDelay()
    {
        _isClearingRows = true;

        for (int y = 0; y < rows; y++)
        {
            if (IsRowFullyMatched(y))
            {
                if (matchLineSpawner != null)
                    matchLineSpawner.SpawnRowLine(this, y);

                AudioManager.Instance?.PlayRowClear();
                yield return new WaitForSeconds(0.35f);
                ShiftRowsUp(y);
                y--; // Kiểm tra lại index y vì dòng y+1 đã nhảy lên y
            }
        }

        _isClearingRows = false;
    }

    private bool IsRowFullyMatched(int y)
    {
        bool hasMatchedCell = false;
        for (int x = 0; x < columns; x++)
        {
            Cellv2 cell = GetCell(x, y);
            if (cell == null || cell.IsEmpty()) continue;
            
            if (cell.isMatched)
            {
                hasMatchedCell = true;
                continue;
            }

            // Nếu gặp bất kỳ ô nào còn số (Normal), thì hàng này chưa xong
            return false;
        }
        // Chỉ coi là "Fully Matched" nếu hàng đó có ít nhất 1 ô đã match (để tránh clear hàng rỗng sẵn)
        return hasMatchedCell;
    }

    private void ShiftRowsUp(int clearedRowY)
    {
        if (gridContainer == null) return;

        // 1. Dọn dẹp dòng bị xoá
        Transform clearedRowTransform = gridContainer.Find($"Row_{clearedRowY}");
        for (int x = 0; x < columns; x++)
        {
            Cellv2 cell = GetCell(x, clearedRowY);
            if (cell != null) cell.SetEmpty();
        }

        // 2. Dịch các dòng bên dưới lên (từ clearedRowY + 1 đến rows - 1)
        for (int y = clearedRowY + 1; y < rows; y++)
        {
            Transform rowToShift = gridContainer.Find($"Row_{y}");

            // Đẩy dữ liệu cell lên 1 dòng trong mảng board
            for (int x = 0; x < columns; x++)
            {
                Cellv2 cellBelow = GetCell(x, y);
                if (cellBelow != null)
                {
                    cellBelow.gridY = y - 1;
                    board[(y - 1) * columns + x] = cellBelow; 
                }
            }

            // Dịch chuyển Transform của cả dòng
            if (rowToShift != null)
            {
                rowToShift.localPosition += new Vector3(0, spacing, 0); 
                rowToShift.name = $"Row_{y - 1}";
            }
        }

        // 3. Xử lý UI cho dòng vừa xoá (đẩy nó xuống làm dòng rỗng cuối cùng)
        if (clearedRowTransform != null)
        {
            clearedRowTransform.localPosition = new Vector3(0, -(rows - 1) * spacing, 0);
            clearedRowTransform.name = $"Row_{rows - 1}";

            // Cập nhật lại mảng board cho dòng cuối cùng
            Cellv2[] clearedCells = clearedRowTransform.GetComponentsInChildren<Cellv2>();
            for (int x = 0; x < columns; x++)
            {
                if (x < clearedCells.Length)
                {
                    clearedCells[x].gridX = x;
                    clearedCells[x].gridY = rows - 1;
                    board[(rows - 1) * columns + x] = clearedCells[x];
                }
            }
        }
    }

    public void CopyAndAppendRemainingNumbers()
    {
        if (addNumber <= 0)
        {
            Debug.Log("Het Luot Chon!"); return;
        }

        AudioManager.Instance?.PlayAddNumber(); // Sound khi bắt đầu thêm số

        // 1. Thu thập các số còn tồn tại trên bảng
        List<Cellv2> newCells = new List<Cellv2>();
        List<int> numbersToCopy = new List<int>();
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                Cellv2 cell = GetCell(x, y);
                if (cell != null && !cell.IsEmpty() && !cell.isMatched)
                    numbersToCopy.Add(cell.numberValue);
            }

        addNumber--;
        if (numbersToCopy.Count == 0) return;

        // 2. Đếm số ô trống hiện có
        int emptySlots = 0;
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                Cellv2 cell = GetCell(x, y);
                if (cell != null && cell.IsEmpty()) emptySlots++;
            }

        // 3. Nếu không đủ chỗ, tính trước số row cần thêm và tạo ngay một lúc
        int deficit = numbersToCopy.Count - emptySlots;
        if (deficit > 0)
        {
            int rowsToAdd = Mathf.CeilToInt((float)deficit / columns);
            for (int i = 0; i < rowsToAdd; i++)
            {
                List<Cellv2> created = AddNewRow();
                if (created.Count == 0) break; // Dừng nếu AddNewRow thất bại
            }
        }

        // 4. Rải tất cả số vào ô trống (kể cả ô mới vừa tạo)
        int copyIndex = 0;
        for (int y = 0; y < rows && copyIndex < numbersToCopy.Count; y++)
            for (int x = 0; x < columns && copyIndex < numbersToCopy.Count; x++)
            {
                Cellv2 cell = GetCell(x, y);
                if (cell != null && cell.IsEmpty())
                {
                    cell.FillData(numbersToCopy[copyIndex]);
                    newCells.Add(cell);
                    copyIndex++;
                }
            }

        // Spawn gem lên các ô mới
        if (GameManager.Instance != null && newCells.Count > 0)
            GameManager.Instance.SpawnGemsOnCells(newCells);

        // Kiểm tra thắng thua
        if (GameManager.Instance != null)
            GameManager.Instance.CheckGameStatus();
    }

    // Tạo 1 row mới gồm đủ 9 ô và gắn vào cuối bảng
    private List<Cellv2> AddNewRow()
    {
        // Lấy prefab từ BoardGeneratorv2 nếu chưa có trực tiếp
        Cellv2 prefab = (boardGenerator != null) ? boardGenerator.cellPrefabv2 : null;
        if (prefab == null)
        {
            Debug.LogError("[GridManager] Không có cellPrefab! Kiểm tra boardGenerator trong Inspector.");
            return new List<Cellv2>();
        }

        int newRowIndex = rows; // Row mới sẽ ở dưới cùng
        rows++;                  // Mở rộng tổng số dòng

        // Mở rộng mảng board
        Cellv2[] newBoard = new Cellv2[rows * columns];
        System.Array.Copy(board, newBoard, board.Length);
        board = newBoard;

        // Tạo container cho row mới
        Transform rowContainer = new GameObject($"Row_{newRowIndex}").transform;
        rowContainer.SetParent(gridContainer);
        rowContainer.localPosition = new Vector3(0, -newRowIndex * spacing, 0);

        List<Cellv2> cells = new List<Cellv2>();
        for (int x = 0; x < columns; x++)
        {
            Cellv2 newCell = UnityEngine.Object.Instantiate(prefab, rowContainer);
            newCell.transform.localPosition = new Vector3(x * spacing, 0, 0);
            newCell.gridX = x;
            newCell.gridY = newRowIndex;
            newCell.SetEmpty();

            board[newRowIndex * columns + x] = newCell;
            cells.Add(newCell);
        }

        Debug.Log($"[GridManager] Đã tạo Row_{newRowIndex} mới. Tổng rows = {rows}");
        return cells;
    }
}
