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

    // Quản lý mảng 1 chiều chứa các ô
    public Cellv2[] board;
    public int addNumber = 6;

    public void Start()
    {
        Screen.SetResolution(1080, 1920, true);
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
            
            // Vì Y đang là số âm (đi xuống), nên để đưa grid về chính giữa:
            // Đẩy trục X lùi về trái, và đẩy trục Y nâng lên trên
            gridContainer.position = new Vector3(-gridWidth / 2f, gridHeight / 2f, 0);
        }
    }

    public void CheckAndClearMatchedRows()
    {
        // Duyệt từ dưới lên trên (để khi dịch dòng xuống không bị lệch index của các dòng chưa kiểm tra)
        for (int y = rows - 1; y >= 0; y--)
        {
            if (IsRowFullyMatched(y))
            {
                ShiftRowsUp(y);
            }
        }
    }

    private bool IsRowFullyMatched(int y)
    {
        for (int x = 0; x < columns; x++)
        {
            Cellv2 cell = GetCell(x, y);
            // Bỏ qua null. Nếu ô không phải matched và không phải empty -> dòng chưa hoàn thành
            if (cell != null && !cell.isMatched && !cell.IsEmpty()) 
            {
                return false;
            }
        }
        return true;
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
        List<int> numbersToCopy = new List<int>();

        if( addNumber <= 0)
        {
            Debug.Log("Het Luot Chon!"); return;
        }

        // 1. Thu thập các số chưa bị loại (từ trên xuống dưới, trái qua phải)
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Cellv2 cell = GetCell(x, y);
                if (cell != null && !cell.IsEmpty() && !cell.isMatched)
                {
                    numbersToCopy.Add(cell.numberValue);
                }
            }
        }
        addNumber--; 
        

        if (numbersToCopy.Count == 0) return;

        // 2. Rải các số này vào các ô trống tiếp theo trên bảng
        int copyIndex = 0;
        List<Cellv2> newCells = new List<Cellv2>();

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Cellv2 cell = GetCell(x, y);
                if (cell != null && cell.IsEmpty())
                {
                    // Hàm FillData sẽ tự kích hoạt lại ô, chuyển state về Normal và đổi Text
                    cell.FillData(numbersToCopy[copyIndex]);
                    newCells.Add(cell);
                    copyIndex++;

                    if (copyIndex >= numbersToCopy.Count)
                    {
                        break; // Đã chép xong toàn bộ
                    }
                }
            }
            if (copyIndex >= numbersToCopy.Count) break;
        }

        if (GameManager.Instance != null && newCells.Count > 0)
        {
            GameManager.Instance.SpawnGemsOnCells(newCells);
        }

        if (copyIndex < numbersToCopy.Count)
        {
            Debug.LogWarning("Bảng đã đầy! Không có đủ ô trống để sao chép toàn bộ các số. Bạn cần thêm cơ chế sinh thêm dòng mới nếu muốn chứa thêm.");
        }

        // Kiểm tra thắng thua sau khi chép số
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckGameStatus();
        }
    }
}
