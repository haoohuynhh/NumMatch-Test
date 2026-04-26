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

    [Header("Gem Goals")]
    public int targetOrange = 5;
    public int targetPurple = 5;
    public int collectedOrange = 0;
    public int collectedPurple = 0;

  

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

                // Gom Gem nếu có
                CollectGem(firstSelectedCell);
                CollectGem(secondSelectedCell);

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

    private void CollectGem(Cellv2 cell)
    {
        if (cell.currentGemType == GemType.Orange)
        {
            collectedOrange++;
            Debug.Log($"Đã thu thập 1 viên Cam! ({collectedOrange}/{targetOrange})");
        }
        else if (cell.currentGemType == GemType.Purple)
        {
            collectedPurple++;
            Debug.Log($"Đã thu thập 1 viên Tím! ({collectedPurple}/{targetPurple})");
        }
    }

    public void SpawnGemsOnCells(List<Cellv2> targetCells, bool isInitialBoard = false)
    {
        int z = 0;
        List<GemType> availableTypes = new List<GemType>();
        
        if (collectedOrange < targetOrange) 
        {
            z++;
            availableTypes.Add(GemType.Orange);
        }
        if (collectedPurple < targetPurple) 
        {
            z++;
            availableTypes.Add(GemType.Purple);
        }

        if (z == 0) return; // Đã gom đủ gem

        List<int> spawnedGemValues = new List<int>();

        if (isInitialBoard)
        {
            // MẶC ĐỊNH SPAWN ĐÚNG Z LOẠI GEM LÚC ĐẦU GAME
            int spawned = 0;
            
            // Xáo trộn mảng để chọn ô ngẫu nhiên
            List<Cellv2> shuffled = new List<Cellv2>(targetCells);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int r = Random.Range(i, shuffled.Count);
                Cellv2 temp = shuffled[i];
                shuffled[i] = shuffled[r];
                shuffled[r] = temp;
            }

            foreach (Cellv2 cell in shuffled)
            {
                if (spawned >= z) break;
                if (cell.IsEmpty() || cell.isMatched || cell.currentGemType != GemType.None) continue;
                
                int val = cell.numberValue;
                bool canSpawn = true;
                foreach (int spawnedVal in spawnedGemValues)
                {
                    if (val == spawnedVal || val + spawnedVal == 10)
                    {
                        canSpawn = false;
                        break;
                    }
                }
                
                if (canSpawn && availableTypes.Count > 0)
                {
                    GemType type = availableTypes[Random.Range(0, availableTypes.Count)];
                    cell.SetGem(type);
                    spawnedGemValues.Add(val);
                    spawned++;
                    availableTypes.Remove(type); // Tránh trùng màu
                }
            }
            return;
        }

        // SPAWN DỰA TRÊN TỈ LỆ VÀ BẢO HIỂM LÚC THÊM SỐ
        int gemsSpawned = 0;
        int yLimit = Mathf.CeilToInt((targetCells.Count + 1) / 2f);
        int currentY = 0;

        for (int i = 0; i < targetCells.Count; i++)
        {
            if (gemsSpawned >= z) break;
            
            Cellv2 cell = targetCells[i];
            if (cell.IsEmpty() || cell.isMatched || cell.currentGemType != GemType.None) continue;

            float rand = Random.Range(0f, 100f);
            bool triggerGem = rand <= 7f || currentY >= yLimit - 1; // 7% tỉ lệ ra ngẫu nhiên hoặc chạm Pity

            if (triggerGem)
            {
                int val = cell.numberValue;
                bool canSpawn = true;
                
                // Kiểm tra xem gem chuẩn bị sinh ra có bị match với gem nào đã đẻ ra cùng đợt không
                foreach (int spawnedVal in spawnedGemValues)
                {
                    if (val == spawnedVal || val + spawnedVal == 10)
                    {
                        canSpawn = false;
                        break;
                    }
                }

                if (canSpawn && availableTypes.Count > 0)
                {
                    GemType type = availableTypes[Random.Range(0, availableTypes.Count)];
                    cell.SetGem(type);
                    spawnedGemValues.Add(val);
                    gemsSpawned++;
                    currentY = 0;
                    availableTypes.Remove(type); // Tránh rải 2 viên cùng màu trong 1 mẻ nếu không cần thiết
                }
                else
                {
                    currentY++;
                }
            }
            else
            {
                currentY++;
            }
        }
    }
}
