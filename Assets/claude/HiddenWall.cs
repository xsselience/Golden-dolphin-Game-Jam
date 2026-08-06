using System.Collections;
using UnityEngine;

/// <summary>
/// 可破坏墙壁 —— 被武器攻击后渐隐消失。
///
/// 用法：
///   1. 创建 GameObject，挂 BoxCollider2D + SpriteRenderer + 本脚本
///   2. Layer 设为武器能打到的层（如 Enemy）
///   3. 可选：设置 linkedZoneId 在被破坏后解锁对应隐藏区域
///
/// 与 v4 的区别：不再参与摄像机系统，纯粹是一面可破坏的墙。
/// 如需解锁隐藏区域，可选填 linkedZoneId。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HiddenWall : MonoBehaviour
{
    [Header("═══ 标识 ═══")]
    [SerializeField] private string wallId = "HiddenWall_00";

    [Header("═══ 可选关联 ═══")]
    [Tooltip("破坏后解锁的 CameraZone ID（可选，留空不触发解锁）")]
    [SerializeField] private string linkedZoneId = "";

    [Header("═══ 消失效果 ═══")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private bool spawnDestroyParticles = true;
    [SerializeField] private GameObject destroyParticlePrefab;
    [SerializeField] private AudioClip destroySfx;
    [SerializeField] private GameObject[] additionalObjectsToHide;

    [Header("═══ 调试 ═══")]
    [SerializeField] private bool debugLog = true;

    private SpriteRenderer _sr;
    private AudioSource _audioSource;
    private bool _destroyed = false;

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        SetupColliders();
        _sr = GetComponent<SpriteRenderer>();
        _audioSource = GetComponent<AudioSource>();

        if (debugLog)
            Debug.Log("[HiddenWall] " + wallId + (string.IsNullOrEmpty(linkedZoneId) ? "" : " → " + linkedZoneId));
    }

    void SetupColliders()
    {
        Collider2D[] all = GetComponents<Collider2D>();
        bool hasNonTrigger = false, hasTrigger = false;
        foreach (Collider2D c in all)
        {
            if (c.isTrigger) hasTrigger = true; else hasNonTrigger = true;
        }
        if (!hasNonTrigger)
        {
            if (all.Length > 0) all[0].isTrigger = false;
            else { BoxCollider2D bc = gameObject.AddComponent<BoxCollider2D>(); bc.isTrigger = false; }
        }
        if (!hasTrigger)
        {
            BoxCollider2D tc = gameObject.AddComponent<BoxCollider2D>();
            tc.isTrigger = true;
            foreach (Collider2D c in GetComponents<Collider2D>())
                if (!c.isTrigger) { tc.size = c.bounds.size * 1.05f; tc.offset = c.offset; break; }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_destroyed) return;
        if (other.GetComponent<playerweapon>() != null)
        {
            if (debugLog) Debug.Log("[HiddenWall] 🗡 " + wallId);
            StartCoroutine(DestroyWall());
        }
    }

    IEnumerator DestroyWall()
    {
        _destroyed = true;
        foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;

        if (destroySfx != null)
        {
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.PlayOneShot(destroySfx);
        }
        if (spawnDestroyParticles && destroyParticlePrefab != null)
            Instantiate(destroyParticlePrefab, transform.position, Quaternion.identity);

        // 可选解锁 CameraZone
        if (!string.IsNullOrEmpty(linkedZoneId) && CameraZoneManager.Instance != null)
            CameraZoneManager.Instance.UnlockZone(linkedZoneId);

        if (_sr != null)
        {
            float t = 0f; Color orig = _sr.color;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                _sr.color = new Color(orig.r, orig.g, orig.b, 1f - t / fadeDuration);
                yield return null;
            }
        }
        else yield return null;

        if (additionalObjectsToHide != null)
            foreach (GameObject o in additionalObjectsToHide) if (o != null) Destroy(o);

        Destroy(gameObject);
    }
}
