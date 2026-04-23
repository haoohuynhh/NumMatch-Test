using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Cell : MonoBehaviour, IPointerClickHandler
{
    public Image image;

    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    private bool isSelected = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectionManager.Instance.Select(this);
        
        
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void SetSelected(bool value)
    {
        isSelected = value;
        UpdateVisual();
    }

    void UpdateVisual()
    {
        image.color = isSelected ? selectedColor : normalColor;
    }
}