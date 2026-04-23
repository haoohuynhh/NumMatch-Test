using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Cellv2 firstSelectedCell;
    public Cellv2 secondSelectedCell;

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
                // Nếu ăn thành công -> Set Matched cả 2 ô
                firstSelectedCell.SetMatched(firstSelectedCell.numberValue);
                secondSelectedCell.SetMatched(secondSelectedCell.numberValue);

                ResetSelection();
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

    private bool CheckValidMatch(Cellv2 cell1, Cellv2 cell2)
    {
        // 1. Kiểm tra giá trị: phải giống nhau hoặc tổng bằng 10
        if (cell1.numberValue != cell2.numberValue && cell1.numberValue + cell2.numberValue != 10)
        {
            Debug.Log("Sai! Giá trị không giống nhau và tổng không bằng 10.");
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
                Debug.Log("Chính xác! Ăn thành công theo đường thẳng (ngang/dọc/chéo).");
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
                Debug.Log("Sai! Đường đi bị chặn bởi một số khác.");
                return false;
            }
        }

        Debug.Log("Chính xác! Ăn thành công theo luật nối đuôi.");
        return true;
    }
}
