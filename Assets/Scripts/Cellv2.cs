using UnityEngine;
using TMPro;

public class Cellv2 : MonoBehaviour
{
    [Header("UI References")]
    public SpriteRenderer background;
    public TMP_Text valueText;

    [Header("Data")]
    public int gridX;
    public int gridY;
    public int numberValue;
    public CellState state;

    public void Setup(int x, int y, int value)
    {
        gridX = x;
        gridY = y;
        
        if (value == 0)
        {
            SetEmpty();
        }
        else
        {
            FillData(value);
        }
    }

    public bool IsEmpty()
    {
        return state == CellState.Empty;
    }
    public bool isMatched
    {
        get { return state == CellState.matched; }
    }

    public void SetMatched(int value)
    {
        state = CellState.matched;
        numberValue = value;
        valueText.color = new Color(valueText.color.r, valueText.color.g,
                            valueText.color.b, 0.30f);
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = false;
    }

    public void SetEmpty()
    {
        state = CellState.Empty;
        numberValue = 0;
        if (valueText != null) valueText.text = "";
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = false;
    }

    // Hàm điền dữ liệu vào ô (Kích hoạt lại ô)
    public void FillData(int value)
    {
        state = CellState.Normal;
        numberValue = value;
        if (valueText != null) 
        {
            valueText.text = value.ToString();
            valueText.color = new Color(valueText.color.r, valueText.color.g, valueText.color.b, 1f);
        }
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = true;
    }

    // public void SelectWrong()
    // {
    //     state = CellState.matching;
    //     if (background != null)
    //     {
    //         background.color = new Color(1f, 0.7f, 0.7f); // Đổi sang màu đỏ nhạt (Highlight sai)
    //     }
    // }

    public void Select()
    {
        state = CellState.matching;
        if (background != null)
        {
            background.color = new Color(0.7f, 1f, 0.7f); 
    }
    }

    public void Deselect()
    {
        if (state == CellState.matching)
        {
            state = CellState.Normal;
        }
        
        if (background != null)
        {
            background.color = Color.white; 
        }
    }

    private void OnMouseDown()
    {
        if (IsEmpty()) return;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCellClicked(this);
        }
        else
        {
            Debug.LogError("Chưa có GameManager trong Scene!");
        }
    }
}
