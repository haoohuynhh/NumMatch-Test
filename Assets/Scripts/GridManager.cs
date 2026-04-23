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
    public Transform gridContainer; // Chuyển từ BoardGenerator sang đây
    public Transform boardBackground; 
    public float padding = 0.5f;      

    // Quản lý mảng 1 chiều chứa các ô
    public Cellv2[] board;

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
}
