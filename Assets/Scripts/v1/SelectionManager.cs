using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;

    private List<Cell> selectedCells = new List<Cell>();

    void Awake()
    {
        Instance = this;
    }

    public void Select(Cell cell)
    {
        // Nếu click lại → bỏ chọn
        if (selectedCells.Contains(cell))
        {
            cell.SetSelected(false);
            selectedCells.Remove(cell);
            return;
        }

        // Nếu đã có 2 → clear hết
        if (selectedCells.Count >= 2)
        {
            foreach (var c in selectedCells)
                c.SetSelected(false);

            selectedCells.Clear();
        }

        // Add mới
        selectedCells.Add(cell);
        cell.SetSelected(true);

        // Nếu đủ 2 → xử lý logic
        if (selectedCells.Count == 2)
        {
            Debug.Log("Đã chọn 2 ô");
        }
    }
}