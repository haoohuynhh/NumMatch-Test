using System.Collections;
using System.Collections.Generic;
// using UnityEditor.SceneManagement;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Cellv2 firstSelectedCell;
    public Cellv2 secondSelectedCell;

    [Header("Game State")]
    [SerializeField] public int currentStage = 1;

    

  

    void Awake()
    {
        
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    public void OnCellClicked(Cellv2 clickedCell)
    {
        Debug.Log($"Đang chọn ô tại toạ độ: (X: {clickedCell.gridX}, Y: {clickedCell.gridY}) - Giá trị: {clickedCell.numberValue}");
    
        if (firstSelectedCell != null && secondSelectedCell != null)
        {
            ResetSelection();
        }

        if (clickedCell == firstSelectedCell)
        {
            firstSelectedCell.Deselect();
            firstSelectedCell = null;
            return; // Dừng tại đây
        }

        if (firstSelectedCell == null)
        {
            firstSelectedCell = clickedCell;
            firstSelectedCell.Select();
        }
        else if (secondSelectedCell == null)
        {
            secondSelectedCell = clickedCell;
            secondSelectedCell.Select();
            
            // GỌI HÀM KIỂM TRA MATCH Ở ĐÂY
            if (CheckValidMatch(firstSelectedCell, secondSelectedCell))
            {
                GridManager grid = firstSelectedCell.GetComponentInParent<GridManager>();

                // Nếu ăn thành công -> Set Matched cả 2 ô
                firstSelectedCell.SetMatched(firstSelectedCell.numberValue);
                secondSelectedCell.SetMatched(secondSelectedCell.numberValue);

                ResetSelection();

                if (grid != null)
                {
                    grid.CheckAndClearMatchedRows();
                }

                // KIỂM TRA THẮNG / THUA SAU KHI ĂN
                CheckGameStatus();
            }
            else
            {
            
                Invoke("ResetSelection", 0.5f); 
                

            }
        }
    }

    public void ResetSelection()
    {
        if (firstSelectedCell != null)
        {
            firstSelectedCell.Deselect();
            firstSelectedCell = null;
        }
        
        if (secondSelectedCell != null)
        {
            secondSelectedCell.Deselect();
            secondSelectedCell = null;
        }
    }

    private bool CheckValidMatch(Cellv2 cell1, Cellv2 cell2, bool silent = false)
    {
        // 1. Kiểm tra giá trị: phải giống nhau hoặc tổng bằng 10
        if (cell1.numberValue != cell2.numberValue && cell1.numberValue + cell2.numberValue != 10)
        {
            if (!silent) Debug.Log("Sai! Giá trị không giống nhau và tổng không bằng 10.");
            return false;
        }

        // Lấy tọa độ
        int x1 = cell1.gridX;
        int y1 = cell1.gridY;
        int x2 = cell2.gridX;
        int y2 = cell2.gridY;

        GridManager gridManager = cell1.GetComponentInParent<GridManager>();

        // Tính khoảng cách
        int dx = x2 - x1;
        int dy = y2 - y1;

        // --- CÁCH 1: KIỂM TRA THẲNG HÀNG (Ngang, Dọc, Chéo) ---
        // Thẳng hàng nếu dx = 0 (dọc), dy = 0 (ngang), hoặc |dx| = |dy| (chéo)
        if (dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy))
        {
            // Tìm hướng đi (step)
            int stepX = (dx == 0) ? 0 : (dx > 0 ? 1 : -1);
            int stepY = (dy == 0) ? 0 : (dy > 0 ? 1 : -1);

            int currentX = x1 + stepX;
            int currentY = y1 + stepY;

            bool isBlocked = false;

            // Duyệt dọc theo đường thẳng từ ô 1 đến ô 2
            while (currentX != x2 || currentY != y2)
            {
                Cellv2 cellInBetween = gridManager.GetCell(currentX, currentY);
                
                if (cellInBetween != null && !cellInBetween.IsEmpty() && !cellInBetween.isMatched)
                {
                    isBlocked = true;
                    break;
                }
                currentX += stepX;
                currentY += stepY;
            }

            if (!isBlocked)
            {
                if (!silent) Debug.Log("Chính xác! Ăn thành công theo đường thẳng (ngang/dọc/chéo).");
                return true;
            }
        }

        // --- CÁCH 2: LUẬT NỐI ĐUÔI (Wrap-around) CỦA NUMBER MATCH ---
        // Quét mảng 1 chiều (từ trái qua phải, trên xuống dưới).
        // Nếu giữa 2 ô toàn là khoảng trống thì ăn được.
        int index1 = y1 * gridManager.columns + x1;
        int index2 = y2 * gridManager.columns + x2;

        int startIndex = Mathf.Min(index1, index2) + 1;
        int endIndex = Mathf.Max(index1, index2);

        for (int i = startIndex; i < endIndex; i++)
        {
            int checkX = i % gridManager.columns;
            int checkY = i / gridManager.columns;
            Cellv2 cellInBetween = gridManager.GetCell(checkX, checkY);
            
            if (cellInBetween != null && !cellInBetween.IsEmpty() && !cellInBetween.isMatched)
            {
                if (!silent) Debug.Log("Sai! Đường đi bị chặn bởi một số khác.");
                return false;
            }
        }

        if (!silent) Debug.Log("Chính xác! Ăn thành công theo luật nối đuôi.");
        return true;
    }

    public void CheckGameStatus()
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null) return;

        bool isCleared = true;
        List<Cellv2> activeCells = new List<Cellv2>();

        // Lấy tất cả các ô còn số
        for (int y = 0; y < gridManager.rows; y++)
        {
            for (int x = 0; x < gridManager.columns; x++)
            {
                Cellv2 cell = gridManager.GetCell(x, y);
                if (cell != null && !cell.IsEmpty() && !cell.isMatched)
                {
                    isCleared = false;
                    activeCells.Add(cell);
                }
            }
        }

        // Win: Nếu bảng không còn số nào
        if (isCleared)
        {
            Debug.Log("CHÚC MỪNG! BẠN ĐÃ CLEAR BẢNG!");
            currentStage++;
            ResetBoard();
            return;
        }

        // Thua: Nếu hết lượt thêm số VÀ không còn cặp nào có thể match
        if (gridManager.addNumber <= 0)
        {
            bool hasMatch = false;
            for (int i = 0; i < activeCells.Count; i++)
            {
                for (int j = i + 1; j < activeCells.Count; j++)
                {
                    if (CheckValidMatch(activeCells[i], activeCells[j], true))
                    {
                        hasMatch = true;
                        break;
                    }
                }
                if (hasMatch) break;
            }

            if (!hasMatch)
            {
                Debug.Log("THUA CUỘC! KHÔNG THỂ MATCH VÀ HẾT LƯỢT THÊM SỐ!");
                ResetBoard();
            }
        }
    }

    private void ResetBoard()
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        BoardGeneratorv2 generator = FindObjectOfType<BoardGeneratorv2>();

        if (gridManager != null && generator != null)
        {
            // Xóa rác cũ
            foreach (Transform child in gridManager.gridContainer)
            {
                Destroy(child.gameObject);
            }
            gridManager.addNumber = 6;
            
            // Gọi sinh bảng mới
            generator.GenerateGrid();
        }
    }
}
