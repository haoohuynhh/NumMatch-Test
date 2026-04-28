using UnityEngine;

public class Cellv2 : MonoBehaviour
{
    [Header("UI References")]
    public SpriteRenderer background;

    [Header("Gem Graphics")]
    public Sprite orangeGemSprite;
    public Sprite purpleGemSprite;
    public Sprite normalSprite;

    [Header("Number Sprites")]
    public SpriteRenderer numberSprite;
    public Sprite number1;
    public Sprite number2;
    public Sprite number3;
    public Sprite number4;
    public Sprite number5;
    public Sprite number6;
    public Sprite number7;
    public Sprite number8;
    public Sprite number9;

    [Header("Data")]
    public int gridX;
    public int gridY;
    public int numberValue;
    public CellState state;
    public GemType currentGemType = GemType.None;

    // ── Scroll guard ────────────────────────────────────────────
    private DragScroll _dragScroll;

    // Lấy sprite số tương ứng với value 1-9
    private Sprite GetNumberSprite(int value)
    {
        switch (value)
        {
            case 1: return number1;
            case 2: return number2;
            case 3: return number3;
            case 4: return number4;
            case 5: return number5;
            case 6: return number6;
            case 7: return number7;
            case 8: return number8;
            case 9: return number9;
            default: return null;
        }
    }

    void Awake()
    {
        // Đảm bảo numberSprite ẩn từ đầu, FillData sẽ bật lại
        if (numberSprite != null) numberSprite.gameObject.SetActive(false);
    }

    void Start()
    {
        // Tìm DragScroll trong scene một lần — có thể ghi đè qua Inspector nếu muốn
        _dragScroll = FindObjectOfType<DragScroll>();
    }

    // Dùng chung background để hiển thị cả normalSprite và gem sprite
    public void SetGem(GemType gemType)
    {
        currentGemType = gemType;

        if (background == null || state == CellState.Empty) return;

        background.color = Color.white;

        if (gemType == GemType.Orange && orangeGemSprite != null)
            background.sprite = orangeGemSprite;
        else if (gemType == GemType.Purple && purpleGemSprite != null)
            background.sprite = purpleGemSprite;
        else if (normalSprite != null)
            background.sprite = normalSprite;
    }

    public void Setup(int x, int y, int value)
    {
        gridX = x;
        gridY = y;

        if (value == 0)
            SetEmpty();
        else
            FillData(value);
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
        // Đổi về normalSprite, làm mờ sprite số
        if (background != null) background.sprite = normalSprite;
        if (numberSprite != null) numberSprite.color = new Color(0f, 0f, 0f, 0.30f);
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = false;


    }

    public void SetEmpty()  
    {
        state = CellState.Empty;
        numberValue = 0;
        currentGemType = GemType.None;
        // Ẩn tất cả
        if (background != null) background.sprite = normalSprite;
        if (numberSprite != null)
        {
            numberSprite.sprite = null;
            numberSprite.color = Color.white;
            numberSprite.gameObject.SetActive(false);
        }
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = false;
    }

    // Hàm điền dữ liệu vào ô (Kích hoạt lại ô)
    public void FillData(int value)
    {
        state = CellState.Normal;
        numberValue = value;
        currentGemType = GemType.None;

        // Hiện background với normalSprite mặc định
        if (background != null)
        {
            background.color = Color.white;
            if (normalSprite != null) background.sprite = normalSprite;
        }

        // Luôn bật và gán sprite số (không cần check null để tránh bỏ sót)
        if (numberSprite != null)
        {
            numberSprite.gameObject.SetActive(true); // << Bật lên dù prefab đang inactive
            
            numberSprite.sprite = GetNumberSprite(value);
            numberSprite.color = Color.black;
        }
        else
        {
            Debug.LogWarning($"[Cellv2] numberSprite chưa được gán trên prefab! Cell ({gridX},{gridY})");
        }

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = true;
    }

    public void Select()
    {
        state = CellState.matching;
        if (background != null)
            background.color = new Color(0.7f, 1f, 0.7f);

        AudioManager.Instance?.PlaySelect();
    }

    public void Deselect()
    {
        if (state == CellState.matching)
            state = CellState.Normal;

        if (background != null)
            background.color = Color.white;
    }

   
    private void OnMouseUp()
    {
        if (IsEmpty()) return;
        if (_dragScroll != null && _dragScroll.IsDragScrolling) return; // đang scroll → bỏ qua

        if (GameManager.Instance != null)
            GameManager.Instance.OnCellClicked(this);
        else
            Debug.LogError("Chưa có GameManager trong Scene!");
    }
}
