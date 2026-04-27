using UnityEngine;

public class DragScroll : MonoBehaviour
{
    [Header("References")]
    public Transform targetContainer;
    public GridManager gridManager;

    [Header("Viewport")]
    [Tooltip("Số row hiển thị trong khung nhìn tại một thời điểm")]
    public int visibleRows = 12;

    [Header("Scroll Settings")]
    public float dragSensitivity = 1f;
    public float inertia = 0.92f;
    public float snapSpeed = 10f;
    public float maxVelocity = 30f; // Giới hạn velocity tối đa để tránh bay ra xa

    // ── Bounds ────────────────────────────────────────────────────
    // _lowerBound: vị trí Y ban đầu, NEO CỐ ĐỊNH, không đổi
    //              → row 0 ở đầu viewport (không scroll lên cao hơn)
    // _upperBound: _lowerBound + extraRows * spacing
    //              → row cuối ở cuối viewport (không scroll xuống thấp hơn)
    // Scroll xuống (thấy row phía dưới) = container.y TĂNG
    private float _lowerBound;
    private float _upperBound;
    private bool  _initialized = false;
    private int   _cachedRows  = -1;

    // ── Drag / Inertia state ───────────────────────────────────────
    private bool  _isDragging   = false;
    private float _lastInputY;
    private float _velocity;
    private bool  _isDragScroll = false;

    /// <summary>Trả về true nếu người dùng đã kéo đủ xa để tính là scroll (không phải click).</summary>
    public  bool  IsDragScrolling => _isDragScroll;

    // ── Init ───────────────────────────────────────────────────────
    void Start()
    {
        StartCoroutine(InitAfterFrame());
    }

    private System.Collections.IEnumerator InitAfterFrame()
    {
        yield return null; // Chờ CenterGrid chạy xong

        if (targetContainer == null) yield break;

        // Neo đầu = vị trí hiện tại sau CenterGrid — không bao giờ thay đổi
        _lowerBound  = targetContainer.position.y;
        _initialized = true;
        RecalculateUpperBound();
    }

    // ── Tính upper bound theo số row ──────────────────────────────
    // Scroll xuống (container.y tăng) tối đa = (totalRows - visibleRows) * spacing
    private void RecalculateUpperBound()
    {
        if (!_initialized || gridManager == null) return;

        _cachedRows = gridManager.rows;
        int   extraRows   = Mathf.Max(0, _cachedRows - visibleRows);
        float scrollRange = extraRows * gridManager.spacing;

        _upperBound = _lowerBound + scrollRange;
    }

    // ── Update ─────────────────────────────────────────────────────
    void Update()
    {
        if (!_initialized) return;

        if (gridManager != null && gridManager.rows != _cachedRows)
            RecalculateUpperBound();

        HandleInput();
        ApplyInertia();
        ClampPosition();
    }

    // ── Input ──────────────────────────────────────────────────────
    private void HandleInput()
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            switch (t.phase)
            {
                case TouchPhase.Began:                   BeginDrag(t.position.y); break;
                case TouchPhase.Moved when _isDragging:  ApplyDrag(t.position.y);  break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:                EndDrag();               break;
            }
            return;
        }

        if      (Input.GetMouseButtonDown(0))            BeginDrag(Input.mousePosition.y);
        else if (Input.GetMouseButton(0) && _isDragging) ApplyDrag(Input.mousePosition.y);
        else if (Input.GetMouseButtonUp(0))              EndDrag();
    }

    private void BeginDrag(float inputY)
    {
        _isDragging   = true;
        _lastInputY   = inputY;
        _velocity     = 0f;
        _isDragScroll = false; // reset mỗi lần bắt đầu drag mới
    }

    private void ApplyDrag(float inputY)
    {
        float deltaPx = inputY - _lastInputY;
        _lastInputY   = inputY;

        // Nếu đã di chuyển đủ ngưỡng → đánh dấu là scroll (không phải click)
        if (Mathf.Abs(deltaPx) > 10f)
            _isDragScroll = true;

        Camera cam  = Camera.main;
        float ppu   = cam != null ? Screen.height / (cam.orthographicSize * 2f) : 100f;
        float delta = (deltaPx / ppu) * dragSensitivity;

        // Rubber-band khi vượt biên
        float nextY = targetContainer.position.y + delta;
        if (nextY < _lowerBound) delta *= 0.2f;
        if (nextY > _upperBound) delta *= 0.2f;

        targetContainer.position += new Vector3(0, delta, 0);
        // Giới hạn velocity tối đa để tránh inertia đẩy object ra vô cực
        float rawVelocity = delta / Mathf.Max(Time.deltaTime, 0.016f);
        _velocity = Mathf.Clamp(rawVelocity, -maxVelocity, maxVelocity);
    }

    private void EndDrag()
    {
        _isDragging = false;
        // _isDragScroll giữ nguyên → Cell đọc tại OnMouseUp, rồi tự reset ở BeginDrag tiếp theo
    }

    // ── Quán tính ──────────────────────────────────────────────────
    private void ApplyInertia()
    {
        if (_isDragging) return;

        if (Mathf.Abs(_velocity) > 0.005f)
        {
            targetContainer.position += new Vector3(0, _velocity * Time.deltaTime, 0);
            _velocity *= inertia;
        }
        else _velocity = 0f;
    }

    // ── Clamp / Bounce-back ────────────────────────────────────────
    private void ClampPosition()
    {
        float cy      = targetContainer.position.y;
        float clamped = Mathf.Clamp(cy, _lowerBound, _upperBound);

        // Trong biên hợp lệ → không làm gì cả
        if (cy >= _lowerBound && cy <= _upperBound) return;

        float dist = Mathf.Abs(cy - clamped);

        if (dist > 1f)
        {
            // Quá xa biên → hard-clamp ngay lập tức để tránh lỗi worldAABB
            targetContainer.position = new Vector3(
                targetContainer.position.x, clamped, targetContainer.position.z);
            _velocity = 0f;
        }
        else
        {
            // Gần biên → Lerp mượt về biên
            float snapped = Mathf.Lerp(cy, clamped, Time.deltaTime * snapSpeed);
            targetContainer.position = new Vector3(
                targetContainer.position.x, snapped, targetContainer.position.z);

            if (_velocity * (cy - clamped) > 0f) _velocity *= 0.5f;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!_initialized) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-5, _lowerBound, 0), new Vector3(5, _lowerBound, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-5, _upperBound, 0), new Vector3(5, _upperBound, 0));
    }
#endif
}
