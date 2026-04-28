using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Cellv2 firstSelectedCell;
    public Cellv2 secondSelectedCell;

    [Header("VFX")]
    public MatchLineSpawner matchLineSpawner;

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
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ── Stage helpers ──────────────────────────────────────────────
    // Gem chi xuat hien tu stage 3 tro len
    private bool IsGemStage() => currentStage >= 3;
    private bool AllGemsCollected() => collectedOrange >= targetOrange && collectedPurple >= targetPurple;

    // ── Cell click / Match ─────────────────────────────────────────
    public void OnCellClicked(Cellv2 clickedCell)
    {
        Debug.Log($"Dang chon o tai toa do: (X: {clickedCell.gridX}, Y: {clickedCell.gridY}) - Gia tri: {clickedCell.numberValue}");

        if (firstSelectedCell != null && secondSelectedCell != null)
            ResetSelection();

        if (clickedCell == firstSelectedCell)
        {
            firstSelectedCell.Deselect();
            firstSelectedCell = null;
            return;
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

            if (CheckValidMatch(firstSelectedCell, secondSelectedCell))
            {
                GridManager grid = firstSelectedCell.GetComponentInParent<GridManager>();

                if (matchLineSpawner != null)
                    matchLineSpawner.SpawnMatchLine(firstSelectedCell, secondSelectedCell, grid);

                firstSelectedCell.SetMatched(firstSelectedCell.numberValue);
                AudioManager.Instance?.PlayMatch();
                secondSelectedCell.SetMatched(secondSelectedCell.numberValue);


                CollectGem(firstSelectedCell);
                CollectGem(secondSelectedCell);

                ResetSelection();

                if (grid != null)
                    grid.CheckAndClearMatchedRows();

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

    // ── Match validation ───────────────────────────────────────────
    public bool CheckValidMatch(Cellv2 cell1, Cellv2 cell2, bool silent = false)
    {
        if (cell1.numberValue != cell2.numberValue && cell1.numberValue + cell2.numberValue != 10)
        {
            if (!silent) Debug.Log("Sai! Gia tri khong giong nhau va tong khong bang 10.");
            return false;
        }

        int x1 = cell1.gridX, y1 = cell1.gridY;
        int x2 = cell2.gridX, y2 = cell2.gridY;
        GridManager gridManager = cell1.GetComponentInParent<GridManager>();
        int dx = x2 - x1, dy = y2 - y1;

        // --- CACH 1: DUONG THANG (Ngang, Doc, Cheo) ---
        if (dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy))
        {
            int stepX = (dx == 0) ? 0 : (dx > 0 ? 1 : -1);
            int stepY = (dy == 0) ? 0 : (dy > 0 ? 1 : -1);
            int cx = x1 + stepX, cy = y1 + stepY;
            bool blocked = false;

            while (cx != x2 || cy != y2)
            {
                Cellv2 mid = gridManager.GetCell(cx, cy);
                if (mid != null && !mid.IsEmpty() && !mid.isMatched) { blocked = true; break; }
                cx += stepX; cy += stepY;
            }

            if (!blocked) { if (!silent) Debug.Log("An theo duong thang!"); return true; }
        }

        // --- CACH 2: NOI DUOI (wrap-around) ---
        int idx1 = y1 * gridManager.columns + x1;
        int idx2 = y2 * gridManager.columns + x2;
        int start = Mathf.Min(idx1, idx2) + 1;
        int end   = Mathf.Max(idx1, idx2);

        for (int i = start; i < end; i++)
        {
            Cellv2 mid = gridManager.GetCell(i % gridManager.columns, i / gridManager.columns);
            if (mid != null && !mid.IsEmpty() && !mid.isMatched)
            {
                if (!silent) Debug.Log("Sai! Duong di bi chan.");
                return false;
            }
        }

        if (!silent) Debug.Log("An theo luat noi duoi!");
        return true;
    }

    // ── Game status ────────────────────────────────────────────────
    public void CheckGameStatus()
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null) return;

        bool isCleared = true;
        List<Cellv2> activeCells = new List<Cellv2>();

        for (int y = 0; y < gridManager.rows; y++)
            for (int x = 0; x < gridManager.columns; x++)
            {
                Cellv2 cell = gridManager.GetCell(x, y);
                if (cell != null && !cell.IsEmpty() && !cell.isMatched)
                {
                    isCleared = false;
                    activeCells.Add(cell);
                }
            }

        // ── BANG DA SACH ───────────────────────────────────────────
        if (isCleared)
        {
            if (!IsGemStage())
            {
                // Stage 1-2: chi can clear bang la thang
                Debug.Log($"STAGE {currentStage} WIN! Bang da sach!");
                currentStage++;
                ResetBoard(win: true);
            }
            else if (AllGemsCollected())
            {
                // Stage 3+: du gem + clear bang = thang
                Debug.Log($"STAGE {currentStage} WIN! Thu du gem va clear bang!");
                currentStage++;
                ResetBoard(win: true);
            }
            else
            {
                // Stage 3+: Clear bang nhung chua du gem -> THUA
                Debug.Log($"STAGE {currentStage} LOSE! Clear bang nhung chua thu du gem! ({collectedOrange}/{targetOrange} cam, {collectedPurple}/{targetPurple} tim)");
                ResetBoard(win: false);
            }
            return;
        }

        // ── THANG GEM TRUOC KHI CLEAR BANG (stage 3+) ─────────────
        if (IsGemStage() && AllGemsCollected())
        {
            Debug.Log($"STAGE {currentStage} WIN! Thu du gem!");
            currentStage++;
            ResetBoard(win: true);
            return;
        }

        // ── HET LUOT THEM SO VA KHONG CON MATCH ───────────────────
        if (gridManager.addNumber <= 0)
        {
            bool hasMatch = false;
            for (int i = 0; i < activeCells.Count && !hasMatch; i++)
                for (int j = i + 1; j < activeCells.Count; j++)
                    if (CheckValidMatch(activeCells[i], activeCells[j], true))
                    { hasMatch = true; break; }

            if (!hasMatch)
            {
                Debug.Log("THUA CUOC! Khong the match va het luot!");
                ResetBoard(win: false);
            }
        }
    }

    // ── Reset ──────────────────────────────────────────────────────
    private void ResetBoard(bool win = false)
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        BoardGeneratorv2 generator = FindObjectOfType<BoardGeneratorv2>();

        if (gridManager != null && generator != null)
        {
            foreach (Transform child in gridManager.gridContainer)
                Destroy(child.gameObject);

            gridManager.addNumber = 6;

            if (win)
                ResetGems(); // Reset gem khi sang stage moi
            // Neu thua: giu stage hien tai, khong reset gem

            generator.GenerateGrid();
        }
    }

    public void AdvanceStageAndReset()
    {
        currentStage++;
        if (currentStage > 3)
            currentStage = 1;

        ResetBoard(win: true);
    }

    private void ResetGems()
    {
        collectedOrange = 0;
        collectedPurple = 0;
        Debug.Log($"[Gem] Reset gem cho stage {currentStage}");
    }

    // ── Gem collection ─────────────────────────────────────────────
    private void CollectGem(Cellv2 cell)
    {
        if (cell.currentGemType == GemType.Orange)
        {
            collectedOrange++;
            Debug.Log($"Thu 1 vien Cam! ({collectedOrange}/{targetOrange})");
        }
        else if (cell.currentGemType == GemType.Purple)
        {
            collectedPurple++;
            Debug.Log($"Thu 1 vien Tim! ({collectedPurple}/{targetPurple})");
        }
    }

    // ── Gem Helpers ───────────────────────────────────────────────
    public void GetExistingGemsCount(out int orangeOnBoard, out int purpleOnBoard)
    {
        orangeOnBoard = 0;
        purpleOnBoard = 0;
        
        GridManager grid = FindObjectOfType<GridManager>();
        if (grid == null || grid.board == null) return;

        foreach (Cellv2 cell in grid.board)
        {
            if (cell == null || cell.IsEmpty() || cell.isMatched) continue;
            if (cell.currentGemType == GemType.Orange) orangeOnBoard++;
            else if (cell.currentGemType == GemType.Purple) purpleOnBoard++;
        }
    }

    // ── Gem spawning ───────────────────────────────────────────────
    public void SpawnGemsOnCells(List<Cellv2> targetCells, bool isInitialBoard = false)
    {
        if (!IsGemStage()) return;

        // 1. Lấy số lượng gem hiện có trên bảng
        int orangeOnBoard, purpleOnBoard;
        GetExistingGemsCount(out orangeOnBoard, out purpleOnBoard);

        // 2. Tính số lượng tối đa có thể sinh thêm mà không vượt quá mục tiêu
        int canSpawnOrange = Mathf.Max(0, targetOrange - (collectedOrange + orangeOnBoard));
        int canSpawnPurple = Mathf.Max(0, targetPurple - (collectedPurple + purpleOnBoard));

        // Nếu cả hai loại đã đủ (hoặc đang có đủ trên bảng) thì không làm gì cả
        if (canSpawnOrange <= 0 && canSpawnPurple <= 0) return;

        // Trộn danh sách để rải gem ngẫu nhiên
        List<Cellv2> shuffled = new List<Cellv2>(targetCells);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int r = Random.Range(i, shuffled.Count);
            Cellv2 tmp = shuffled[i]; shuffled[i] = shuffled[r]; shuffled[r] = tmp;
        }

        int orangeSpawned = 0;
        int purpleSpawned = 0;
        List<int> spawnedValues = new List<int>();

        // X% random 5-7%, Y pity, Z toi da = so loai gem con thieu
        int availableTypes = 0;
        if (canSpawnOrange > 0) availableTypes++;
        if (canSpawnPurple > 0) availableTypes++;
        int maxGemsThisTurn = availableTypes;
        if (maxGemsThisTurn <= 0) return;

        int pityLimit = Mathf.CeilToInt((targetCells.Count + 1) / 2f);
        int sinceLastGem = 0;
        int gemsSpawned = 0;

        foreach (Cellv2 cell in shuffled)
        {
            if (gemsSpawned >= maxGemsThisTurn) break;
            if (cell.IsEmpty() || cell.isMatched || cell.currentGemType != GemType.None) continue;

            sinceLastGem++;

            float chance = Random.Range(5f, 7f);
            bool pityTrigger = sinceLastGem >= pityLimit;
            bool trigger = pityTrigger || (Random.Range(0f, 100f) <= chance);

            if (!trigger) continue;

            int val = cell.numberValue;
            bool valueConflict = false;
            foreach (int sv in spawnedValues)
                if (val == sv || val + sv == 10) { valueConflict = true; break; }
            if (valueConflict) continue;

            List<GemType> possibleTypes = new List<GemType>();
            if (orangeSpawned < canSpawnOrange) possibleTypes.Add(GemType.Orange);
            if (purpleSpawned < canSpawnPurple) possibleTypes.Add(GemType.Purple);

            if (possibleTypes.Count > 0)
            {
                GemType selected = possibleTypes[Random.Range(0, possibleTypes.Count)];
                cell.SetGem(selected);
                spawnedValues.Add(val);
                gemsSpawned++;
                sinceLastGem = 0;

                if (selected == GemType.Orange) orangeSpawned++;
                else purpleSpawned++;
            }
        }
    }
}
