using System.Collections;
using UnityEngine;

/// <summary>
/// 单侧门及其控制器
///
/// 同一 GameObject 上挂两个 Collider2D：
///   - 第一个（非 Trigger）：物理阻挡玩家
///   - 第二个（Trigger）：检测武器命中
///
/// 两种模式：
///   Simple —— 此物体就是门本体，被攻击后消失/移动
///   Controller —— 此物体是开关，攻击后遥控 Linked Doors 打开
///
/// 两种开门方式：
///   Dissolve —— 渐隐后销毁
///   Move —— 平滑移动后销毁
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OneWayDoor : MonoBehaviour
{
    [Header("═══ 模式 ═══")]
    [Tooltip("Simple —— 这就是门本身\nController —— 这是开关，攻击后 Linked Doors 打开")]
    [SerializeField] private DoorMode mode = DoorMode.Simple;

    [Header("═══ 关联的门（仅 Controller 模式） ═══")]
    [Tooltip("拖入要打开的门。门的碰撞体会在开门时被禁用。")]
    [SerializeField] private GameObject[] linkedDoors;

    [Header("═══ 开门方式 ═══")]
    [Tooltip("Dissolve: 渐隐后销毁  |  Move: 平滑移动后销毁")]
    [SerializeField] private DoorOpenType openType = DoorOpenType.Dissolve;

    [Header("═══ 溶解/消失效果 ═══")]
    [Tooltip("渐隐时长（秒），0 = 立即消失")]
    [SerializeField] private float dissolveDuration = 0.6f;

    [Header("═══ 位移效果（仅 Move 模式生效） ═══")]
    [Tooltip("门移动的目标偏移量")]
    [SerializeField] private Vector3 moveOffset = new Vector3(0, 3, 0);

    [Tooltip("门移动速度")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("═══ 音效与粒子 ═══")]
    [Tooltip("开门音效")]
    [SerializeField] private AudioClip openSfx;

    [Tooltip("开门粒子预制体")]
    [SerializeField] private GameObject openParticlePrefab;

    [Header("═══ 延迟销毁 ═══")]
    [Tooltip("打开后多少秒销毁自身（0 = 不自动销毁）")]
    [SerializeField] private float destroyAfterOpen = 0f;

    // 内部状态
    private bool _opened = false;
    private AudioSource _audioSource;

    // ———————————————— Unity 生命周期 ————————————————

    void Start()
    {
        // 设置刚体
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        // 确保有非 Trigger 碰撞体（阻挡）+ Trigger 碰撞体（检测武器）
        SetupColliders();

        _audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 确保同一个 GameObject 上有：
    ///   至少一个非 Trigger Collider（阻挡玩家）
    ///   至少一个 Trigger Collider（检测武器）
    /// </summary>
    void SetupColliders()
    {
        Collider2D[] allColliders = GetComponents<Collider2D>();
        bool hasTrigger = false;
        bool hasNonTrigger = false;

        foreach (Collider2D col in allColliders)
        {
            if (col.isTrigger) hasTrigger = true;
            else hasNonTrigger = true;
        }

        if (!hasNonTrigger)
        {
            if (allColliders.Length > 0)
                allColliders[0].isTrigger = false;
            else
            {
                BoxCollider2D bc = gameObject.AddComponent<BoxCollider2D>();
                bc.isTrigger = false;
            }
        }

        if (!hasTrigger)
        {
            BoxCollider2D triggerCol = gameObject.AddComponent<BoxCollider2D>();
            triggerCol.isTrigger = true;

            // 大小参考非 Trigger 碰撞体
            Collider2D[] updated = GetComponents<Collider2D>();
            foreach (Collider2D col in updated)
            {
                if (!col.isTrigger)
                {
                    Bounds b = col.bounds;
                    triggerCol.size = b.size * 1.05f;
                    triggerCol.offset = col.offset;
                    break;
                }
            }
        }
    }

    // ———————————————— 武器命中检测 ————————————————

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_opened) return;

        playerweapon weapon = other.GetComponent<playerweapon>();
        if (weapon != null)
        {
            Debug.Log("[OneWayDoor] 武器命中 [" + gameObject.name + "] → 开门");
            StartCoroutine(OpenDoor());
        }
    }

    // ———————————————— 公共 API ————————————————

    /// <summary>
    /// 外部也可调用开门（按钮/事件系统等）
    /// </summary>
    public void Open()
    {
        if (!_opened)
            StartCoroutine(OpenDoor());
    }

    // ———————————————— 开门流程 ————————————————

    IEnumerator OpenDoor()
    {
        _opened = true;

        // 禁用自身所有碰撞体
        Collider2D[] allCols = GetComponents<Collider2D>();
        foreach (Collider2D col in allCols)
            col.enabled = false;

        // 音效
        if (openSfx != null)
        {
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.PlayOneShot(openSfx);
        }

        // 粒子
        if (openParticlePrefab != null)
        {
            Instantiate(openParticlePrefab, transform.position, Quaternion.identity);
        }

        // 处理关联的门
        if (mode == DoorMode.Controller && linkedDoors != null)
        {
            foreach (GameObject doorObj in linkedDoors)
            {
                if (doorObj != null)
                    yield return StartCoroutine(OpenSingleDoor(doorObj));
            }
        }
        else
        {
            yield return StartCoroutine(OpenSingleDoor(gameObject));
        }

        // 延迟销毁
        if (destroyAfterOpen > 0)
        {
            yield return new WaitForSeconds(destroyAfterOpen);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 对指定门物体执行打开操作
    /// </summary>
    IEnumerator OpenSingleDoor(GameObject doorObj)
    {
        if (doorObj == null) yield break;

        // 禁用所有碰撞体
        Collider2D[] cols = doorObj.GetComponents<Collider2D>();
        foreach (Collider2D c in cols)
            c.enabled = false;

        if (openType == DoorOpenType.Dissolve)
        {
            SpriteRenderer sr = doorObj.GetComponent<SpriteRenderer>();
            if (sr != null && dissolveDuration > 0)
            {
                float elapsed = 0f;
                Color orig = sr.color;
                while (elapsed < dissolveDuration)
                {
                    elapsed += Time.deltaTime;
                    float a = Mathf.Lerp(1f, 0f, elapsed / dissolveDuration);
                    sr.color = new Color(orig.r, orig.g, orig.b, a);
                    yield return null;
                }
            }
            Destroy(doorObj);
        }
        else // Move
        {
            Vector3 targetPos = doorObj.transform.position + moveOffset;

            while (Vector3.Distance(doorObj.transform.position, targetPos) > 0.02f)
            {
                doorObj.transform.position = Vector3.MoveTowards(
                    doorObj.transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            Destroy(doorObj);
        }
    }

    // ———————————————— 枚举 ————————————————

    public enum DoorMode
    {
        [Tooltip("此物体就是门本身")]
        Simple,
        [Tooltip("此物体是开关，攻击后 Linked Doors 打开")]
        Controller
    }

    public enum DoorOpenType
    {
        [Tooltip("渐隐后销毁")]
        Dissolve,
        [Tooltip("平滑移动到目标位置后销毁")]
        Move
    }

    // ———————————————— Scene 视图 Gizmos ————————————————

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (mode == DoorMode.Controller && linkedDoors != null)
        {
            Gizmos.color = Color.cyan;
            foreach (GameObject d in linkedDoors)
            {
                if (d != null)
                    Gizmos.DrawLine(transform.position, d.transform.position);
            }
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.4f,
                "🚪 控制器 → " + linkedDoors.Length + " 扇门");
        }

        if (openType == DoorOpenType.Move)
        {
            Gizmos.color = Color.yellow;
            Vector3 target = transform.position + moveOffset;
            Gizmos.DrawLine(transform.position, target);
            Gizmos.DrawWireCube(target, Vector3.one * 0.2f);
        }
    }
#endif
}
