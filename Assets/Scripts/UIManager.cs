using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;   

    [Header("Add-Number UI")]
    public TMP_Text addNumberText;

    [Header("Stage UI")]
    public TMP_Text stageText;

    [Header("Gem Goal UI")]
    public TMP_Text orangeGemText;
    public TMP_Text purpleGemText;
    public GameObject gemDisplay;

    // ── Update ─────────────────────────────────────────────────────────
    void Update()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (addNumberText != null && gridManager != null)
        {
            addNumberText.text = $"{gridManager.addNumber}";
        }
        if (GameManager.Instance == null) return;

        if (gemDisplay != null)
        {
            bool showGems = GameManager.Instance.currentStage >= 3;
            if (gemDisplay.activeSelf != showGems)
                gemDisplay.SetActive(showGems);
        }

        if (stageText != null)
        {
            stageText.text = $"Stage:{GameManager.Instance.currentStage}";
        }

        if (orangeGemText != null)
        {
            orangeGemText.text =
                $"{GameManager.Instance.collectedOrange}/{GameManager.Instance.targetOrange}";
        }

        if (purpleGemText != null)
        {
            purpleGemText.text =
                $"{GameManager.Instance.collectedPurple}/{GameManager.Instance.targetPurple}";
        }
    }
}
