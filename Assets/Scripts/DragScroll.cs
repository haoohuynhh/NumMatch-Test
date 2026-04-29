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
    public float hardClampDistance = 5f; // Vuot qua khoang nay se snap ve bien de tranh worldAABB

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
    private float _snapVelocity;

    public  bool  IsDragScrolling => _isDragScroll;

    void Start()
    {
        StartCoroutine(InitAfterFrame());
    }

    private System.Collections.IEnumerator InitAfterFrame()
    {
        yield return null; // Chờ CenterGrid chạy xong

        if (targetContainer == null) yield break;

        // Neo đầu = vị trí hiện tại sau CenterGrid — không bao giờ thay đổi
        _lowerBound  = targetContainer.localPosition.y;
        _initialized = true;
        RecalculateUpperBound();
    }


    private void RecalculateUpperBound()
    {
        if (!_initialized || gridManager == null) return;

        _cachedRows = gridManager.rows;
        int   extraRows   = Mathf.Max(0, _cachedRows - visibleRows);
        float scrollRange = extraRows * gridManager.spacing;

        _upperBound = _lowerBound + scrollRange;
    }
    public void ResetScroll()
    {
        if (targetContainer == null) return;
        _lowerBound  = targetContainer.localPosition.y;
        _initialized = true;
        RecalculateUpperBound();
    }

    void Update()
    {
        if (!_initialized) return;

        if (gridManager != null && gridManager.rows != _cachedRows)
            RecalculateUpperBound();

        HandleInput();
        ApplyInertia();
        ClampPosition();
    }

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

        if (Mathf.Abs(deltaPx) > 10f)
            _isDragScroll = true;

        Camera cam  = Camera.main;
        float ppu   = cam != null ? Screen.height / (cam.orthographicSize * 2f) : 100f;
        float delta = (deltaPx / ppu) * dragSensitivity;

        // Chặn delta không vượt quá một ngưỡng an toàn trong 1 frame để tránh lỗi văng do lướt quá nhanh
        delta = Mathf.Clamp(delta, -5f, 5f);

        float nextY = targetContainer.localPosition.y + delta;
        if (nextY < _lowerBound) delta *= 0.2f;
        if (nextY > _upperBound) delta *= 0.2f;

        targetContainer.localPosition += new Vector3(0, delta, 0);
        float rawVelocity = delta / Mathf.Max(Time.deltaTime, 0.016f);
        _velocity = Mathf.Clamp(rawVelocity, -maxVelocity, maxVelocity);
    }

    private void EndDrag()
    {
        _isDragging = false;
    }

    private void ApplyInertia()
    {
        if (_isDragging) return;

        if (Mathf.Abs(_velocity) > 0.005f)
        {
            targetContainer.localPosition += new Vector3(0, _velocity * Time.deltaTime, 0);
            
            // Tính toán quán tính độc lập với frame rate
            float cy = targetContainer.localPosition.y;
            bool outOfBounds = cy < _lowerBound || cy > _upperBound;
            
            // Phanh gấp nếu ra khỏi vùng an toàn
            float currentFriction = outOfBounds ? 0.2f : inertia;
            
            _velocity *= Mathf.Pow(currentFriction, Time.deltaTime * 60f);
        }
        else 
        {
            _velocity = 0f;
        }
    }

    private void ClampPosition()
    {
        float cy      = targetContainer.localPosition.y;
        float clamped = Mathf.Clamp(cy, _lowerBound, _upperBound);

        if (cy >= _lowerBound && cy <= _upperBound) return;

        float dist = Mathf.Abs(cy - clamped);
        
        // Giới hạn không cho vượt qua hardClampDistance để tránh bay quá xa, 
        // nhưng không đưa thẳng về 'clamped' ngay lập tức để tránh giật hình.
        if (dist > hardClampDistance)
        {
            cy = clamped + Mathf.Sign(cy - clamped) * hardClampDistance;
            
            // Xóa velocity hướng ra ngoài để không tiếp tục đẩy xa hơn
            if (_velocity * (cy - clamped) > 0f) 
                _velocity = 0f;
        }

        float smoothTime = 1f / Mathf.Max(snapSpeed, 0.01f);
        float snapped = Mathf.SmoothDamp(cy, clamped, ref _snapVelocity, smoothTime);
        
        targetContainer.localPosition = new Vector3(
            targetContainer.localPosition.x, snapped, targetContainer.localPosition.z);
    }
}
