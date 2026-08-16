using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class CameraZoneManager : MonoBehaviour
{
    [Header("摄像机")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private bool autoFindCamera = true;

    [Header("自动房间检测（不用传送门也能切换相机边界）")]
    public bool autoDetectPlayerZone = true;
    public float zoneCheckInterval = 0.3f; // 每0.3秒检测一次
    private float _zoneCheckTimer;

    [Header("黑屏过渡")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float holdDuration = 0.2f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float freezeTimeScale = 0f;

    [Header("黑色遮罩")]
    [SerializeField] private GameObject darkOverlayPrefab;
    [SerializeField] private float darkOverlayFadeDuration = 1.0f;

    [Header("自动解锁")]
    [SerializeField] private bool autoUnlockOnEnter = true;

    [Header("Confiner设置")]
    [SerializeField] private float confinerDamping = 0.5f;

    [Header("调试")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool _showMainCameraBounds = true;
    [SerializeField] private Color _cameraBoundsColor = Color.red;

    [Header("原地探头设置")]
    public float peekHoldTime = 0.5f;
    public float peekOffsetAmount = 0.5f;
    public float peekSmoothSpeed = 5f;

    private CinemachineFramingTransposer _framingTransposer;
    private Vector2 _targetPeekOffset;
    private Vector2 _currentPeekOffset;

    private CinemachineConfiner2D _confiner2D;
    private GameObject _cameraBoundsObj;
    private LineRenderer _cameraBoundsLine;
    private Dictionary<string, CameraZone> _zones = new Dictionary<string, CameraZone>();
    private Dictionary<string, GameObject> _darkOverlays = new Dictionary<string, GameObject>();
    private CameraZone _currentZone;
    private Transform _player;
    private bool _isTransitioning;


    /// <summary>自动检测玩家现在处于哪一个CameraZone区域</summary>
    private CameraZone GetPlayerCurrentZone()
    {
        if (_player == null) return null;
        Vector2 playerPos = _player.position;

        // 只要玩家仍在当前区域内，就保持当前区域不切换。
        // 这样区域1和区域2重合时，玩家要走完重合区（彻底离开区域1）才会切到区域2。
        if (_currentZone != null && IsPointInPolygon(playerPos, _currentZone))
            return _currentZone;

        foreach (var kvp in _zones)
        {
            CameraZone zone = kvp.Value;
            if (zone == null || zone.boundsPolygon == null) continue;
            if (IsPointInPolygon(playerPos, zone))
            {
                return zone;
            }
        }
        return null;
    }

    /// <summary>设置探头偏移方向</summary>
    public void SetPeekDirection(Vector2 dir)
    {
        dir = Vector2.ClampMagnitude(dir, 1f);
        _targetPeekOffset = dir * peekOffsetAmount;
    }
    /// <summary>取消探头，视角回归居中</summary>
    public void ClearPeek()
    {
        _targetPeekOffset = Vector2.zero;
    }

    public static CameraZoneManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.color = Color.clear;
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }

        InitCamera();
        CreateCameraBoundsGizmo();
        StartCoroutine(DelayedInit());
    }

    void LateUpdate()
    {
        // 探头视角平滑插值
        if (_framingTransposer != null)
        {
            _currentPeekOffset = Vector2.Lerp(_currentPeekOffset, _targetPeekOffset, Time.deltaTime * peekSmoothSpeed);
            _framingTransposer.m_ScreenX = 0.5f + _currentPeekOffset.x;
            _framingTransposer.m_ScreenY = 0.5f + _currentPeekOffset.y;
        }

        if (_confiner2D == null || _player == null || _isTransitioning)
        {
            UpdateCameraBoundsGizmo();
            return;
        }

        CheckAutoUnlock();

        // ==========【新增】玩家位置自动检测房间，不用传送门也切换相机边界 ==========
        if (autoDetectPlayerZone)
        {
            _zoneCheckTimer -= Time.deltaTime;
            if (_zoneCheckTimer <= 0f)
            {
                _zoneCheckTimer = zoneCheckInterval;
                CameraZone detectedZone = GetPlayerCurrentZone();
                // 如果检测到的区域 和 当前激活的区域不一样，则切换（走路穿越相邻房间，只平滑切换边界，不黑屏）
                if (detectedZone != null && detectedZone != _currentZone)
                {
                    if (debugLog) Debug.Log($"[CameraZoneManager] 玩家自动进入房间：{detectedZone.zoneDisplayName}");
                    SwitchToZoneImmediate(detectedZone);
                }
            }
        }
        // =====================================================================

        UpdateCameraBoundsGizmo();
    }

    void InitCamera()
    {
        if (autoFindCamera && virtualCamera == null)
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (virtualCamera == null)
        {
            Debug.LogError("[CameraZoneManager] 未找到 CinemachineVirtualCamera");
            return;
        }

        _confiner2D = virtualCamera.GetComponent<CinemachineConfiner2D>();
        if (_confiner2D == null)
            _confiner2D = virtualCamera.gameObject.AddComponent<CinemachineConfiner2D>();

        _confiner2D.m_Damping = confinerDamping;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        _framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (_framingTransposer == null)
            Debug.LogError("VCam Body必须设置为Framing Transposer才能使用探头功能！");
    }
    IEnumerator DelayedInit()
    {
        yield return null;
        yield return null;

        CreateOverlaysForHiddenZones();

        if (_currentZone == null)
        {
            foreach (CameraZone z in _zones.Values)
            {
                if (!z.startsHidden)
                {
                    SwitchToZoneImmediate(z);
                    break;
                }
            }

            if (_currentZone == null && _zones.Count > 0)
            {
                using var e = _zones.Values.GetEnumerator();
                e.MoveNext();
                SwitchToZoneImmediate(e.Current);
            }
        }

        if (debugLog)
            Debug.Log($"[CameraZoneManager] 初始化完成，共 {_zones.Count} 个区域");
    }

    void CreateOverlaysForHiddenZones()
    {
        foreach (CameraZone zone in _zones.Values)
        {
            if (zone.startsHidden)
                CreateDarkOverlay(zone);
        }
    }

    void CreateDarkOverlay(CameraZone zone)
    {
        if (darkOverlayPrefab == null) return;
        if (_darkOverlays.ContainsKey(zone.zoneId)) return;

        GameObject overlay = Instantiate(darkOverlayPrefab, transform);
        overlay.name = $"DarkOverlay_{zone.zoneDisplayName}";

        PolygonCollider2D poly = zone.boundsPolygon;
        if (poly != null)
        {
            Vector2[] pts = poly.GetPath(0);
            if (pts.Length > 0)
            {
                Vector2 min = zone.transform.TransformPoint(pts[0]);
                Vector2 max = min;
                for (int i = 1; i < pts.Length; i++)
                {
                    Vector2 wp = zone.transform.TransformPoint(pts[i]);
                    min = Vector2.Min(min, wp);
                    max = Vector2.Max(max, wp);
                }
                overlay.transform.position = (min + max) * 0.5f;
                SpriteRenderer sr = overlay.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.size = max - min;
                    sr.sortingOrder = 100;
                }
            }
        }

        _darkOverlays[zone.zoneId] = overlay;
    }

    public void RegisterZone(CameraZone zone)
    {
        if (string.IsNullOrEmpty(zone.zoneId)) return;
        if (_zones.ContainsKey(zone.zoneId)) return;
        _zones[zone.zoneId] = zone;
    }

    public void TriggerRoomTransition(RoomGate gate)
    {
        if (_isTransitioning) return;
        if (!_zones.TryGetValue(gate.targetZoneId, out CameraZone targetZone))
        {
            Debug.LogError($"[CameraZoneManager] 目标区域不存在：{gate.targetZoneId}");
            return;
        }

        gate.GetComponent<Collider2D>().enabled = false;

        player playerCtrl = _player.GetComponent<player>();
        float moveDir = Mathf.Sign(playerCtrl.number);
        if (Mathf.Abs(moveDir) < 0.01f)
            moveDir = _player.localScale.x > 0 ? 1f : -1f;

        playerCtrl.StartRoomTransition(moveDir);

        StartCoroutine(RoomTransitionRoutine(gate, targetZone));
    }

    void KillPlayerVelocity()
    {
        if (_player == null) return;
        Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
    }

    IEnumerator RoomTransitionRoutine(RoomGate gate, CameraZone targetZone)
    {
        _isTransitioning = true;

        yield return StartCoroutine(FadeScreen(Color.black, fadeOutDuration));

        KillPlayerVelocity();

        _player.position = gate.targetSpawnPoint.transform.position;
        SwitchToZoneImmediate(targetZone);

        yield return null;
        yield return null;

        if (!targetZone.isUnlocked && targetZone.startsHidden)
            UnlockZone(targetZone.zoneId);

        if (holdDuration > 0)
            yield return new WaitForSecondsRealtime(holdDuration);

        yield return StartCoroutine(FadeScreen(Color.clear, fadeInDuration));

        _player.GetComponent<player>().EndRoomTransition();

        _isTransitioning = false;
        gate.GetComponent<Collider2D>().enabled = true;

        if (debugLog)
            Debug.Log($"[CameraZoneManager] 传送完成：{targetZone.zoneDisplayName}");
    }

    IEnumerator FadeScreen(Color targetColor, float duration)
    {
        if (fadeImage == null)
        {
            yield return new WaitForSecondsRealtime(duration);
            yield break;
        }

        if (!fadeImage.gameObject.activeSelf)
        {
            fadeImage.raycastTarget = false;
            fadeImage.color = Color.clear;
            fadeImage.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        Color start = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeImage.color = Color.Lerp(start, targetColor, elapsed / duration);
            yield return null;
        }

        fadeImage.color = targetColor;
        if (targetColor.a < 0.01f)
            fadeImage.gameObject.SetActive(false);
    }

    void SwitchToZoneImmediate(CameraZone zone)
    {
        _targetPeekOffset = Vector2.zero;
        if (zone == null || zone.boundsPolygon == null) return;
        _currentZone = zone;
        UpdateConfinerBounds(zone);
    }

    void UpdateConfinerBounds(CameraZone zone)
    {
        if (_confiner2D == null || zone.boundsPolygon == null) return;
        _confiner2D.m_BoundingShape2D = zone.boundsPolygon;
        // ⭐关键：必须刷新Confiner缓存，否则新多边形不生效！！
        _confiner2D.InvalidateCache();
    }

    

    void CheckAutoUnlock()
    {
        if (!autoUnlockOnEnter) return;

        foreach (CameraZone zone in _zones.Values)
        {
            if (!zone.startsHidden || zone.isUnlocked) continue;
            if (IsPointInPolygon(_player.position, zone))
                UnlockZone(zone.zoneId);
        }
    }

    public void UnlockZone(string zoneId)
    {
        if (!_zones.TryGetValue(zoneId, out CameraZone zone)) return;
        if (zone.isUnlocked) return;

        zone.isUnlocked = true;
        if (_darkOverlays.TryGetValue(zoneId, out GameObject overlay) && overlay != null)
            StartCoroutine(FadeOutDarkOverlay(overlay, darkOverlayFadeDuration));

        if (debugLog)
            Debug.Log($"[CameraZoneManager] 解锁区域：{zone.zoneDisplayName}");
    }

    IEnumerator FadeOutDarkOverlay(GameObject overlay, float duration)
    {
        SpriteRenderer sr = overlay.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Destroy(overlay);
            yield break;
        }

        float elapsed = 0f;
        Color start = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(start.a, 0f, elapsed / duration);
            sr.color = new Color(start.r, start.g, alpha);
            yield return null;
        }

        Destroy(overlay);
    }

    public void ForceSwitchZone(string zoneId)
    {
        if (_zones.TryGetValue(zoneId, out CameraZone zone))
            SwitchToZoneImmediate(zone);
    }

    bool IsPointInPolygon(Vector2 point, CameraZone zone)
    {
        if (zone.boundsPolygon == null) return false;
        Vector2[] pts = zone.GetPath();
        if (pts.Length < 3) return false;

        bool inside = false;
        int j = pts.Length - 1;

        for (int i = 0; i < pts.Length; j = i++)
        {
            Vector2 pi = zone.transform.TransformPoint(pts[i]);
            Vector2 pj = zone.transform.TransformPoint(pts[j]);

            if (((pi.y > point.y) != (pj.y > point.y))
                && (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public CameraZone GetCurrentZone() => _currentZone;

    /// <summary>
    /// 创建一个 LineRenderer 子物体，用于在场景中画出摄像机可见范围框（不依赖 Gizmos，始终可见）
    /// </summary>
    void CreateCameraBoundsGizmo()
    {
        if (_cameraBoundsObj != null) return;

        _cameraBoundsObj = new GameObject("CameraBoundsGizmo");
        _cameraBoundsObj.transform.SetParent(transform);
        _cameraBoundsLine = _cameraBoundsObj.AddComponent<LineRenderer>();

        // 配置 LineRenderer 为矩形框（4 个角 + 闭合回到起点 = 5 个点）
        _cameraBoundsLine.positionCount = 5;
        _cameraBoundsLine.loop = false;
        _cameraBoundsLine.startWidth = 0.1f;
        _cameraBoundsLine.endWidth = 0.1f;
        _cameraBoundsLine.material = new Material(Shader.Find("Sprites/Default"));
        _cameraBoundsLine.startColor = _cameraBoundsColor;
        _cameraBoundsLine.endColor = _cameraBoundsColor;
        _cameraBoundsLine.sortingOrder = 9999; // 保证在最顶层显示
        _cameraBoundsLine.useWorldSpace = true;

        UpdateCameraBoundsGizmo();
    }

    /// <summary>
    /// 每帧更新摄像机范围框的位置和大小，使其跟随摄像机
    /// </summary>
    void UpdateCameraBoundsGizmo()
    {
        if (_cameraBoundsLine == null || !_showMainCameraBounds) return;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return; // 2D游戏只用正交摄像机

        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        Vector3 pos = cam.transform.position;

        _cameraBoundsLine.enabled = true;

        // 矩形：左上 → 右上 → 右下 → 左下 → 回到左上
        _cameraBoundsLine.SetPosition(0, new Vector3(pos.x - w, pos.y + h, 0));
        _cameraBoundsLine.SetPosition(1, new Vector3(pos.x + w, pos.y + h, 0));
        _cameraBoundsLine.SetPosition(2, new Vector3(pos.x + w, pos.y - h, 0));
        _cameraBoundsLine.SetPosition(3, new Vector3(pos.x - w, pos.y - h, 0));
        _cameraBoundsLine.SetPosition(4, new Vector3(pos.x - w, pos.y + h, 0));
    }

    void OnDestroy()
    {
        if (_cameraBoundsObj != null)
            Destroy(_cameraBoundsObj);
    }

}