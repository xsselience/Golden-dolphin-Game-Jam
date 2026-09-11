using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// 地刺陷阱（SpikeTrap）—— 玩家碰到后：受击 → 击退回弹 → 黑屏 → 传回安全点。
public class SpikeTrap:MonoBehaviour
{
    [Header("伤害")]
    public int damage = 10;

    [Header("传送点")]
    [Tooltip("玩家被传回的位置")]
    public Transform safe_point;
    [Tooltip("安全点所在的 CameraZone zoneId，传送后切换相机到该区域（可留空）")]
    public string safeZoneId = "";

    [Header("═══ 击退回弹 ═══")]
    [Tooltip("是否启用碰到时回弹")]
    public bool enableKnockback = true;
    [Tooltip("水平回弹力度（会乘以方向，弹离陷阱）")]
    public float knockbackForceX = 8f;
    [Tooltip("垂直回弹力度（正值=向上）")]
    public float knockbackForceY = 6f;

    [Header("═══ 黑屏过渡 ═══")]
    [Tooltip("拖入 Canvas 下的黑色 Image 用于黑屏过渡")]
    public Image fadeImage;
    public float fadeOutDuration = 0.3f;
    public float holdDuration = 0.15f;
    public float fadeInDuration = 0.3f;

    [Header("═══ 触发检测 ═══")]
    [Tooltip("用 Player 标签判断进入者")]
    public bool usePlayerTag = true;
    private bool _isTrapActive = false; // 陷阱是否正在处理中（防止重复触发）

    private void Start()
    {
        Collider2D col=GetComponent<Collider2D>();
        if (col == null )
            col.isTrigger = true;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(_isTrapActive)
            return;
        if (!other.CompareTag("Player"))
            return;
        StartCoroutine(TrapRoutine(other));
        IEnumerator TrapRoutine(Collider2D playerCol)
        {
            _isTrapActive = true;

            GameObject playerObj = playerCol.gameObject;
            player playerScript = playerObj.GetComponent<player>();
            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();

            // ═══ 1. 扣血（预留：取消注释即可接入） ═══
            // if (playerScript != null) playerScript.TakeDamage(damage);

            // ═══ 2. 受击动画（预留：在玩家 Animator 里新建一个 "hurt" 触发器后取消注释） ═══
            // Animator anim = playerObj.GetComponent<Animator>();
            // if (anim != null) anim.SetTrigger("hurt");

            // ═══ 3. 击退回弹 ═══
            if (enableKnockback && rb != null)
            {
                // 水平方向：玩家在陷阱右边就向右弹，左边就向左弹（弹离陷阱）
                float dirX = playerObj.transform.position.x >= transform.position.x ? 1f : -1f;

                // 锁定玩家输入一小段时间，防止回弹速度被 move() 立即覆盖
                if (playerScript != null) playerScript.isKnockedBack = true;

                rb.velocity = new Vector2(dirX * knockbackForceX, knockbackForceY);
            }

            // ═══ 4. 短暂停顿，让击退/受击表现一下 ═══
            yield return new WaitForSeconds(0.15f);

            // ═══ 5. 黑屏淡入 ═══
            yield return StartCoroutine(FadeToBlack(fadeOutDuration));

            // ═══ 6. 传送回安全点 ═══
            if (safe_point != null)
                playerObj.transform.position = safe_point.position;
            if (rb != null) rb.velocity = Vector2.zero;

            // ═══ 7. 恢复玩家输入（解除击退锁定） ═══
            if (playerScript != null) playerScript.isKnockedBack = false;

            // ═══ 8. 切换相机到安全区域（可选） ═══
            if (!string.IsNullOrEmpty(safeZoneId) && CameraZoneManager.Instance != null)
                CameraZoneManager.Instance.ForceSwitchZone(safeZoneId);

            // ═══ 9. 等待 + 黑屏淡出 ═══
            if (holdDuration > 0)
                yield return new WaitForSecondsRealtime(holdDuration);
            yield return StartCoroutine(FadeToClear(fadeInDuration));

            _isTrapActive = false;
        }

        // ═══ 黑屏辅助（与 RoomConnectionManager 里的一致） ═══
        IEnumerator FadeToBlack(float duration)
        {
            if (fadeImage == null) { yield return new WaitForSecondsRealtime(duration); yield break; }

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
                fadeImage.color = Color.Lerp(start, Color.black, elapsed / duration);
                yield return null;
            }
            fadeImage.color = Color.black;
        }

        IEnumerator FadeToClear(float duration)
        {
            if (fadeImage == null) { yield return new WaitForSecondsRealtime(duration); yield break; }

            float elapsed = 0f;
            Color start = fadeImage.color;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeImage.color = Color.Lerp(start, Color.clear, elapsed / duration);
                yield return null;
            }
            fadeImage.color = Color.clear;
            fadeImage.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            }
            if (safe_point != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, safe_point.position);
                Gizmos.DrawWireSphere(safe_point.position, 0.3f);
            }
        }
#endif

    }
}


