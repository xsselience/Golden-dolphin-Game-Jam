using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("伤害数值")]
    [SerializeField] private int damageToPlayer;   // a：打到玩家时的伤害（敌人武器用）
    [SerializeField] private int damageToPlayerMax;
    [SerializeField] private int damageToEnemy;    // b：打到敌人时的伤害（玩家武器用）

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;         // Player 图层
    [SerializeField] private LayerMask enemyLayer;          // Enemy 图层

    /// <summary>
    /// 碰撞检测（2D）
    /// 如果用的是 3D，把 OnTriggerEnter2D 改成 OnTriggerEnter，
    /// 把 Collider2D 改成 Collider。
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject);
    }

    /// <summary>
    /// 物理碰撞检测（2D），如果你没用 Trigger 而是实体碰撞
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    /// <summary>
    /// 统一处理命中逻辑
    /// </summary>
    void HandleHit(GameObject hitObject)
    {
        int hitLayer = 1 << hitObject.layer;

        // ── 打到玩家 ──//防御判定在这改（应该）
        if ((hitLayer & playerLayer) != 0)
        {
            player playerHealth = hitObject.GetComponent<player>();//hitObject的意思是击中判定
            if (playerHealth != null)
            {
                int finalDamage = TakeDamageWithParry(damageToPlayer, playerHealth);
                playerHealth.TakeDamage(finalDamage);   // 用 a 值
            }
            else
            {
                damageToPlayer = damageToPlayerMax;
            }
            
        }

        // ── 打到敌人 ──
        if ((hitLayer & enemyLayer) != 0)
        {
            enemy enemyHealth = hitObject.GetComponent<enemy>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damageToEnemy);     // 用 b 值
            }
        }
    }

    public int TakeDamageWithParry(int incomingDamage,player playerHealth)
    {
        if (!playerHealth.isBlocking)
        {
            // 没防御 → 全额受伤
            Debug.Log("没防御！");
            return incomingDamage;
        }

        if (playerHealth.perfectActive)
        {
            // 完美弹反！无伤 + 敌人停滞
            StartCoroutine(HitStop());
            Debug.Log("完美弹反！");
            return 0;
        }
        else
        {
            // 普通格挡 → 减免伤害
            int reduced = Mathf.RoundToInt(incomingDamage * (1f - playerHealth.blockDamageReduction));
            Debug.Log($"格挡！伤害 {incomingDamage} → {reduced}");
            return reduced;
        }
    }

    IEnumerator HitStop()
    {
        Time.timeScale = 0f;                            // 暂停
        yield return new WaitForSecondsRealtime(0.1f);  // 停 0.05 秒（用真实时间，不受暂停影响）
        Time.timeScale = 1f;                            // 恢复
    }
}
