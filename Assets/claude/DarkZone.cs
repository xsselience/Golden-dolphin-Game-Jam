using System.Collections;
using UnityEngine;

/// <summary>
/// 黑暗遮罩区域（DarkZone）
/// 作用：盖在世界空间上的一块黑布，用来遮挡视野死角、地板下空地、隐藏房间等。
/// 形状由 PolygonCollider2D 定义（支持任意不规则多边形，房间外形不固定也能贴合）。
/// 注意：本组件与 CameraZoneManager 的遮罩体系（darkOverlayPrefab）完全独立，互不影响。
///
/// 支持三种模式（_mode）：
///   1. FadeOutOnEnter  进入后渐隐消失（一次性，模拟"发现隐藏房间"）
///   2. RepeatableFade  进入渐隐、离开恢复，可反复进出（模拟"可反复探索的黑暗角落"）
///   3. VisionHole      常驻黑幕，玩家周围出现一个圆形亮区（视野），其余黑幕保持不变
///                     （模拟"探索地下室"：只有角色身边一小圈可见）
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class DarkZone : MonoBehaviour
{
    /// <summary>黑暗区域的行为模式</summary>
    public enum DarkZoneMode
    {
        FadeOutOnEnter, // 进入后渐隐消失（一次性）
        RepeatableFade, // 进入渐隐、离开恢复，可反复
        VisionHole      // 圆形视野：玩家周围亮、其余黑幕保持
    }

    [Header("【行为模式】")]
    [Tooltip("选择该黑暗区域的触发行为")]
    [SerializeField] private DarkZoneMode _mode = DarkZoneMode.FadeOutOnEnter;

    [Header("【遮罩外观】")]
    [Tooltip("用于渲染的方块 Sprite（白色方块即可，颜色由下方 _darkColor 染色）。留空则自动生成。注意：圆形视野模式会自动使用纯白方块，忽略此项")]
    [SerializeField] private Sprite _darkSprite;

    [Tooltip("遮罩颜色与不透明度，默认不透明纯黑。想要半透明黑幕可调低 Alpha")]
    [SerializeField] private Color _darkColor = new Color(0f, 0f, 0f, 1f);

    [Tooltip("渲染排序层级，数值越大越显示在上层。默认 100 覆盖大多数场景物体")]
    [SerializeField] private int _sortingOrder = 100;

    [Header("【边缘柔化（仅进入消失/可重复两种模式，且未拖入 _darkSprite 时生效）】")]
    [Tooltip("勾选后遮罩边缘渐隐（羽化），看起来不突兀、不孤立")]
    [SerializeField] private bool _softEdges = true;

    [Tooltip("边缘羽化程度 0~1。0=硬边，1=从中心就开始渐隐，数值越大边缘越柔和")]
    [SerializeField] private float _edgeFeather = 0.35f;

    [Header("【渐隐过渡】")]
    [Tooltip("渐隐时长（秒），进入消失和可重复两种模式都用到")]
    [SerializeField] private float _fadeDuration = 0.5f;

    [Tooltip("渐隐完成后是否销毁整个 DarkZone 物体（仅进入消失模式有效）。勾选则玩家发现后彻底移除该区域")]
    [SerializeField] private bool _destroyAfterFade = true;

    [Header("【圆形视野模式参数】")]
    [Tooltip("玩家周围的圆形视野半径（世界单位），数值越大能看到的范围越大")]
    [SerializeField] private float _visionRadius = 3f;

    [Tooltip("圆形视野边缘的羽化宽度（世界单位），让亮区到黑幕过渡更柔和")]
    [SerializeField] private float _visionFeather = 1.5f;

    [Header("【触发检测】")]
    [Tooltip("用 Player 标签判断进入者（推荐）。若你的玩家不是 Player 标签，取消勾选并改用下方 LayerMask")]
    [SerializeField] private bool _usePlayerTag = true;

    [Tooltip("仅当上方 _usePlayerTag 为 false 时生效：指定玩家所在的 Layer")]
    [SerializeField] private LayerMask _playerLayerMask;

    private PolygonCollider2D _polygon;
    private SpriteRenderer _sr;
    private Sprite _generatedSoftSprite; // 缓存的程序化柔边方块，避免重复生成
    private Material _visionMaterial;    // 圆形视野模式使用的材质
    private Coroutine _fadeRoutine;      // 可重复模式中，用于停止上一次渐隐协程
    private Transform _player;           // 进入的玩家引用
    private bool _playerInside;          // 玩家是否在当前区域内（圆形视野模式用）

    void Awake()
    {
        _polygon = GetComponent<PolygonCollider2D>();
        // 关键：作为触发器，玩家可穿过并触发 OnTriggerEnter2D / OnTriggerExit2D
        _polygon.isTrigger = true;

        CreateVisual();
    }

    void Update()
    {
        // 仅圆形视野模式需要每帧更新玩家位置，让圆形亮区跟随角色移动
        if (_mode != DarkZoneMode.VisionHole || _visionMaterial == null) return;

        if (_playerInside && _player != null)
        {
            // 玩家在区域内：设置圆形视野中心为玩家位置
            _visionMaterial.SetVector("_PlayerPos", new Vector4(_player.position.x, _player.position.y, 0f, 0f));
            _visionMaterial.SetFloat("_Radius", _visionRadius);
            _visionMaterial.SetFloat("_Feather", Mathf.Max(0.01f, _visionFeather));
        }
        else
        {
            // 玩家不在区域内：半径归零，整块黑幕全黑（无圆形亮区）
            _visionMaterial.SetFloat("_Radius", 0f);
        }
    }

    /// <summary>
    /// 创建一个子物体挂 SpriteRenderer，用黑色矩形覆盖多边形的轴对齐包围盒。
    /// 圆形视野模式额外挂载自定义 Shader 材质实现"玩家周围挖洞"效果。
    /// </summary>
    private void CreateVisual()
    {
        Bounds bounds = _polygon.bounds;

        // 子物体承载视觉，避免移动 DarkZone 自身影响 Collider
        GameObject visualObj = new GameObject("DarkVisual");
        visualObj.transform.SetParent(transform, false);
        visualObj.transform.position = bounds.center; // 对齐到包围盒中心

        _sr = visualObj.AddComponent<SpriteRenderer>();
        _sr.sprite = GetDarkSprite();
        _sr.color = _darkColor;
        _sr.sortingOrder = _sortingOrder;

        // 圆形视野模式：挂载挖洞 Shader
        if (_mode == DarkZoneMode.VisionHole)
        {
            Shader shader = Shader.Find("Custom/DarkZoneVision");
            if (shader != null)
            {
                _visionMaterial = new Material(shader);
                _sr.material = _visionMaterial;
                // 初始设为无视野（全黑），等玩家进入后由 Update 更新
                _visionMaterial.SetFloat("_Radius", 0f);
                _visionMaterial.SetFloat("_Feather", Mathf.Max(0.01f, _visionFeather));
            }
            else
            {
                Debug.LogError("[DarkZone] 未找到 Shader: Custom/DarkZoneVision，请确认 DarkZoneVision.shader 已导入项目。圆形视野模式将退化为普通黑幕。");
            }
        }

        // ⭐关键修复：SpriteRenderer 在 Simple（默认）绘制模式下设置 .size 无效，会被忽略，
        // 导致方块只以 sprite 原生大小渲染（所以之前只有中心一小点黑）。
        // 正确做法：用 sprite 原生世界尺寸做基准，缩放 transform 让方块恰好铺满碰撞包围盒。
        Vector2 nativeSize = _sr.sprite.bounds.size;
        Vector3 scale = visualObj.transform.localScale;
        if (nativeSize.x > 0.0001f && nativeSize.y > 0.0001f)
        {
            scale.x = bounds.size.x / nativeSize.x;
            scale.y = bounds.size.y / nativeSize.y;
        }
        visualObj.transform.localScale = scale;
    }

    /// <summary>获取用于渲染的方块 Sprite。圆形视野模式用纯白方块，其余模式优先用拖入的图或程序化柔边方块</summary>
    private Sprite GetDarkSprite()
    {
        if (_darkSprite != null) return _darkSprite;

        // 圆形视野模式：洞由 Shader 计算，Sprite 只需纯白矩形
        if (_mode == DarkZoneMode.VisionHole)
            return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

        // 开启柔边时，程序化生成一张边缘渐隐的方块；否则用纯白 1x1 方块（硬边）
        if (_softEdges)
        {
            if (_generatedSoftSprite == null)
                _generatedSoftSprite = GenerateSoftEdgeSprite(128, _edgeFeather);
            return _generatedSoftSprite;
        }
        return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    /// <summary>程序化生成一张边缘渐隐的白色方块 Sprite（白色 + alpha 羽化，最终颜色由 _darkColor 染色）</summary>
    private static Sprite GenerateSoftEdgeSprite(int size, float feather)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear; // 双线性过滤，让羽化过渡更平滑

        float featherPx = Mathf.Clamp(feather, 0.01f, 1f) * size * 0.5f; // 羽化宽度（像素）

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 到矩形四边的最短距离（像素）：边缘为0，中心最大
                float dx = Mathf.Min(x, size - 1 - x);
                float dy = Mathf.Min(y, size - 1 - y);
                float dist = Mathf.Min(dx, dy);

                // 边缘 alpha=0，向内逐渐到 1，形成柔滑边界
                float alpha = Mathf.Clamp01(dist / featherPx);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        // pixelsPerUnit=100：128 像素原生尺寸 1.28 世界单位，铺满时按比例缩放
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _player = other.transform;

        switch (_mode)
        {
            case DarkZoneMode.FadeOutOnEnter:
                // 一次性：进入即渐隐消失（防止重复触发）
                if (_fadeRoutine == null)
                    _fadeRoutine = StartCoroutine(FadeOutAndDisappear());
                break;

            case DarkZoneMode.RepeatableFade:
                // 可重复：停止上次恢复，重新渐隐到透明
                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
                _fadeRoutine = StartCoroutine(FadeAlphaTo(0f, _fadeDuration));
                break;

            case DarkZoneMode.VisionHole:
                // 圆形视野：标记玩家进入，由 Update 每帧更新亮区位置
                _playerInside = true;
                break;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _player = null;

        switch (_mode)
        {
            case DarkZoneMode.RepeatableFade:
                // 离开：渐显恢复黑幕，可再次进入
                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
                _fadeRoutine = StartCoroutine(FadeAlphaTo(_darkColor.a, _fadeDuration));
                break;

            case DarkZoneMode.VisionHole:
                // 圆形视野：玩家离开，圆形亮区消失（全黑）
                _playerInside = false;
                break;
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
            Destroy(gameObject);   // 彻底移除整个区域（含 Collider 和视觉）
        else
            _sr.enabled = false;   // 只隐藏视觉，保留触发区
    }

    /// <summary>通用渐隐协程：把遮罩 alpha 平滑过渡到目标值</summary>
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

    /// <summary>在 Scene 视图中用半透明色块显示遮罩范围（选中物体时可见）</summary>
    private void OnDrawGizmosSelected()
    {
        if (_polygon == null) _polygon = GetComponent<PolygonCollider2D>();
        if (_polygon == null) return;

        Bounds b = _polygon.bounds;
        Gizmos.color = new Color(0f, 0f, 0f, 0.5f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(b.center, b.size);
    }

    #endregion
}
