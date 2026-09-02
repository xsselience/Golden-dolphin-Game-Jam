using System.Collections;
using UnityEngine;

/// <summary>
/// 多边形黑区（PolygonDarkZone）—— 遮罩精确贴合 PolygonCollider2D 绘制的任意多边形形状。
/// 渲染原理：把多边形光栅化到一张 Texture2D（判断每个像素是否在多边形内），再用 SpriteRenderer 显示。
/// 颜色直接烘焙进像素，不依赖 Mesh 的 UV 或自定义 shader，稳定可靠。
///
/// 支持两种模式（_mode）：
///   1. FadeOutOnEnter  进入后渐隐消失（一次性，模拟"发现隐藏房间"）
///   2. RepeatableFade  进入渐隐、离开恢复，可反复进出
///
/// 使用：
///   1. 新建空物体，挂 PolygonCollider2D（勾选 Is Trigger），用 Edit Collider 画出黑区形状
///   2. 挂本脚本，选模式、调颜色
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class PolygonDarkZone : MonoBehaviour
{
    /// <summary>行为模式</summary>
    public enum Mode
    {
        FadeOutOnEnter, // 进入后渐隐消失（一次性）
        RepeatableFade  // 进入渐隐、离开恢复，可反复
    }

    [Header("【行为模式】")]
    [Tooltip("FadeOutOnEnter=进入后渐隐消失；RepeatableFade=进入渐隐、离开恢复")]
    [SerializeField] private Mode _mode = Mode.FadeOutOnEnter;

    [Header("【遮罩外观】")]
    [Tooltip("遮罩颜色与不透明度，默认不透明纯黑。想要半透明黑幕可调低 Alpha")]
    [SerializeField] private Color _darkColor = new Color(0f, 0f, 0f, 1f);

    [Tooltip("渲染排序层级，数值越大越显示在上层。默认 100 覆盖大多数场景物体")]
    [SerializeField] private int _sortingOrder = 100;

    [Tooltip("光栅化分辨率：每世界单位多少像素。数值越大边缘越精细，但内存越高。默认 32")]
    [SerializeField] private int _pixelsPerUnit = 32;

    [Header("【边缘柔化】")]
    [Tooltip("勾选后遮罩边缘渐隐（羽化），看起来不突兀、不孤立")]
    [SerializeField] private bool _softEdges = true;

    [Tooltip("边缘羽化宽度（世界单位）。数值越大边缘越柔和。默认 0.5")]
    [SerializeField] private float _edgeFeather = 0.5f;

    [Header("【渐隐过渡】")]
    [Tooltip("渐隐时长（秒）")]
    [SerializeField] private float _fadeDuration = 0.5f;

    [Tooltip("渐隐完成后是否销毁整个物体（仅进入消失模式有效）")]
    [SerializeField] private bool _destroyAfterFade = true;

    [Header("【触发检测】")]
    [Tooltip("用 Player 标签判断进入者。若玩家不是 Player 标签，取消勾选并改用下方 LayerMask")]
    [SerializeField] private bool _usePlayerTag = true;

    [Tooltip("仅当上方 _usePlayerTag 为 false 时生效：指定玩家所在的 Layer")]
    [SerializeField] private LayerMask _playerLayerMask;

    private PolygonCollider2D _polygon;
    private SpriteRenderer _sr;
    private Coroutine _fadeRoutine;

    void Awake()
    {
        _polygon = GetComponent<PolygonCollider2D>();
        _polygon.isTrigger = true;

        BuildVisual();
    }

    /// <summary>
    /// 把多边形光栅化到 Texture2D 并生成 Sprite，用 SpriteRenderer 显示。
    /// 颜色烘焙进像素，SpriteRenderer.color 用于渐隐。
    /// </summary>
    private void BuildVisual()
    {
        // 获取多边形世界坐标顶点（用于点是否在多边形内的判断）
        Vector2[] localPts = _polygon.GetPath(0);
        if (localPts.Length < 3) return;

        Vector2[] worldPts = new Vector2[localPts.Length];
        for (int i = 0; i < localPts.Length; i++)
            worldPts[i] = transform.TransformPoint(localPts[i]);

        // 世界轴对齐包围盒
        Bounds bounds = _polygon.bounds;

        int ppu = Mathf.Max(1, _pixelsPerUnit);
        int texW = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x * ppu));
        int texH = Mathf.Max(1, Mathf.CeilToInt(bounds.size.y * ppu));

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color inside = _darkColor;                    // 多边形内：遮罩颜色
        Color outside = new Color(0f, 0f, 0f, 0f);    // 多边形外：全透明

        float featherPx = _softEdges ? _edgeFeather * ppu : 0f; // 羽化宽度（像素）

        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                // 该像素中心对应的世界坐标
                Vector2 world = new Vector2(
                    bounds.min.x + (x + 0.5f) / texW * bounds.size.x,
                    bounds.min.y + (y + 0.5f) / texH * bounds.size.y
                );

                bool inPoly = IsPointInPolygon(world, worldPts);

                if (!inPoly)
                {
                    tex.SetPixel(x, y, outside);
                    continue;
                }

                // 在多边形内：若开启柔边，计算到边界的最短距离，靠近边界 alpha 渐降
                if (_softEdges && featherPx > 0.001f)
                {
                    float distWorld = DistanceToPolygonEdge(world, worldPts);
                    float alpha = Mathf.Clamp01(distWorld / _edgeFeather);
                    tex.SetPixel(x, y, new Color(_darkColor.r, _darkColor.g, _darkColor.b, _darkColor.a * alpha));
                }
                else
                {
                    tex.SetPixel(x, y, inside);
                }
            }
        }
        tex.Apply();

        // 生成 Sprite：pivot 居中，pixelsPerUnit=ppu，原生尺寸约等于 bounds.size
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, texW, texH), new Vector2(0.5f, 0.5f), ppu);

        // 子物体承载视觉，位置对齐到包围盒中心
        GameObject visualObj = new GameObject("PolygonDarkVisual");
        visualObj.transform.SetParent(transform, false);
        visualObj.transform.position = bounds.center;

        _sr = visualObj.AddComponent<SpriteRenderer>();
        _sr.sprite = sprite;
        _sr.color = Color.white;   // 颜色已烘焙进纹理，这里保持白色，渐隐时改这里的 alpha
        _sr.sortingOrder = _sortingOrder;
    }

    /// <summary>射线法判断点是否在多边形内（顶点为世界坐标）</summary>
    private bool IsPointInPolygon(Vector2 point, Vector2[] pts)
    {
        bool inside = false;
        int j = pts.Length - 1;
        for (int i = 0; i < pts.Length; j = i++)
        {
            if (((pts[i].y > point.y) != (pts[j].y > point.y))
                && (point.x < (pts[j].x - pts[i].x) * (point.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    /// <summary>计算点到多边形每条边的最短距离（世界单位），用于边缘羽化</summary>
    private float DistanceToPolygonEdge(Vector2 point, Vector2[] pts)
    {
        float minDist = float.MaxValue;
        for (int i = 0; i < pts.Length; i++)
        {
            Vector2 a = pts[i];
            Vector2 b = pts[(i + 1) % pts.Length];
            minDist = Mathf.Min(minDist, DistanceToSegment(point, a, b));
        }
        return minDist;
    }

    /// <summary>计算点到线段的最短距离</summary>
    private float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSqr = ab.sqrMagnitude;
        if (lenSqr < 0.000001f) return Vector2.Distance(p, a); // a、b 重合

        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSqr);
        Vector2 projection = a + t * ab;
        return Vector2.Distance(p, projection);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        switch (_mode)
        {
            case Mode.FadeOutOnEnter:
                if (_fadeRoutine == null)
                    _fadeRoutine = StartCoroutine(FadeOutAndDisappear());
                break;

            case Mode.RepeatableFade:
                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
                _fadeRoutine = StartCoroutine(FadeAlphaTo(0f, _fadeDuration));
                break;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        if (_mode == Mode.RepeatableFade)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeAlphaTo(1f, _fadeDuration));
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        return _usePlayerTag ? other.CompareTag("Player") : IsInPlayerLayer(other);
    }

    private bool IsInPlayerLayer(Collider2D other)
    {
        return (_playerLayerMask.value & (1 << other.gameObject.layer)) != 0;
    }

    /// <summary>进入消失模式：渐隐到透明，然后按设置销毁或隐藏</summary>
    private IEnumerator FadeOutAndDisappear()
    {
        yield return StartCoroutine(FadeAlphaTo(0f, _fadeDuration));

        if (_destroyAfterFade)
            Destroy(gameObject);          // 彻底移除整个区域（含 Collider 和视觉）
        else
            _sr.enabled = false;           // 只隐藏视觉，保留触发区
    }

    /// <summary>通用渐隐：把 SpriteRenderer 的 alpha 平滑过渡到目标值</summary>
    private IEnumerator FadeAlphaTo(float targetAlpha, float duration)
    {
        if (_sr == null) yield break;

        Color start = _sr.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(start.a, targetAlpha, elapsed / duration);
            _sr.color = new Color(start.r, start.g, start.b, a);
            yield return null;
        }

        _sr.color = new Color(start.r, start.g, start.b, targetAlpha);
    }

    #region Scene 视图调试绘制

    private void OnDrawGizmosSelected()
    {
        if (_polygon == null) _polygon = GetComponent<PolygonCollider2D>();
        if (_polygon == null) return;

        Vector2[] pts = _polygon.GetPath(0);
        if (pts.Length < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < pts.Length; i++)
        {
            Vector3 a = transform.TransformPoint(pts[i]);
            Vector3 b = transform.TransformPoint(pts[(i + 1) % pts.Length]);
            Gizmos.DrawLine(a, b);
        }
    }

    #endregion
}
