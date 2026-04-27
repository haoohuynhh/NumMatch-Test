using System.Collections;
using System.Collections.Generic;
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

                firstSelectedCell.SetMatched(firstSelectedCell.numberValue);
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

    // ── Gem spawning ───────────────────────────────────────────────
    public void SpawnGemsOnCells(List<Cellv2> targetCells, bool isInitialBoard = false)
    {
        // Gem chi xuat hien tu stage 3 tro len
        if (!IsGemStage()) return;

        int z = 0;
        List<GemType> availableTypes = new List<GemType>();

        if (collectedOrange < targetOrange) { z++; availableTypes.Add(GemType.Orange); }
        if (collectedPurple < targetPurple) { z++; availableTypes.Add(GemType.Purple); }

        if (z == 0) return; // Da gom du gem

        List<int> spawnedGemValues = new List<int>();

        if (isInitialBoard)
        {
            // Ep buoc spawn dung Z gem luc dau game
            int spawned = 0;
            List<Cellv2> shuffled = new List<Cellv2>(targetCells);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int r = Random.Range(i, shuffled.Count);
                Cellv2 tmp = shuffled[i]; shuffled[i] = shuffled[r]; shuffled[r] = tmp;
            }

            foreach (Cellv2 cell in shuffled)
            {
                if (spawned >= z) break;
                if (cell.IsEmpty() || cell.isMatched || cell.currentGemType != GemType.None) continue;

                int val = cell.numberValue;
                bool canSpawn = true;
                foreach (int sv in spawnedGemValues)
                    if (val == sv || val + sv == 10) { canSpawn = false; break; }

                if (canSpawn && availableTypes.Count > 0)
                {
                    GemType type = availableTypes[Random.Range(0, availableTypes.Count)];
                    cell.SetGem(type);
                    spawnedGemValues.Add(val);
                    spawned++;
                    availableTypes.Remove(type);
                }
            }
            return;
        }

        // Spawn theo ti le + bao hiem khi them so
        int gemsSpawned = 0;
        int yLimit = Mathf.CeilToInt((targetCells.Count + 1) / 2f);
        int currentY = 0;

        for (int i = 0; i < targetCells.Count; i++)
        {
            if (gemsSpawned >= z) break;

            Cellv2 cell = targetCells[i];
            currentY++;

            if (cell.IsEmpty() || cell.isMatched || cell.currentGemType != GemType.None) continue;

            float rand = Random.Range(0f, 100f);
            bool pity  = (currentY >= yLimit - 1);
            bool trigger = rand <= 7f || pity;

            if (trigger)
            {
                int val = cell.numberValue;
                bool canSpawn = true;
                foreach (int sv in spawnedGemValues)
                    if (val == sv || val + sv == 10) { canSpawn = false; break; }

                if (canSpawn && availableTypes.Count > 0)
                {
                    GemType type = availableTypes[Random.Range(0, availableTypes.Count)];
                    cell.SetGem(type);
                    spawnedGemValues.Add(val);
                    gemsSpawned++;
                    currentY = 0; // Reset bao hiem
                }
            }
        }
    }
}
