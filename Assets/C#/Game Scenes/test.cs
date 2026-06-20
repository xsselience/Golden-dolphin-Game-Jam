using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    [Header("完美弹反窗口")]
    public float perfectWindow = 0.2f;   // 右键按下后多久是完美弹反

    [Header("普通格挡")]
    public float blockDamageReduction = 0.5f;  // 格挡减免比例 (0.5=减半)

    [Header("敌人停滞")]
    public float stunDuration = 0.8f;

    bool isBlocking;         // 右键按住
    bool perfectActive;      // 还在完美窗口内

    void Update()
    {
        // 按下右键 → 完美弹反开始
        if (Input.GetMouseButtonDown(1))
        {
            isBlocking = true;
            perfectActive = true;
            StartCoroutine(PerfectWindowTimer());
        }

        // 松开右键 → 取消
        if (Input.GetMouseButtonUp(1))
        {
            isBlocking = false;
            perfectActive = false;
        }
    }

    IEnumerator PerfectWindowTimer()
    {
        yield return new WaitForSeconds(perfectWindow);
        perfectActive = false;  // 完美窗口关闭，进入普通格挡
    }

    // ===== 受伤入口（敌人攻击时调用这个，替代直接扣血）=====
    public int TakeDamageWithParry(int damage, GameObject enemy)
    {
        if (!isBlocking)
        {
            // 没防御 → 全额受伤
            return damage;
        }

        if (perfectActive)
        {
            // 完美弹反！无伤 + 敌人停滞
            StartCoroutine(HitStop());
            StartCoroutine(StunEnemy(enemy));
            Debug.Log("完美弹反！");
            return 0;
        }
        else
        {
            // 普通格挡 → 减免伤害
            int reduced = Mathf.RoundToInt(damage * (1f - blockDamageReduction));
            Debug.Log($"格挡！伤害 {damage} → {reduced}");
            return reduced;
        }
    }

    IEnumerator StunEnemy(GameObject enemy)
    {
        if (enemy == null) yield break;

        var rb = enemy.GetComponent<Rigidbody2D>();
        var ai = enemy.GetComponent<enemy>();  // 你自己的敌人脚本

        // 冻结敌人
        if (rb != null) rb.velocity = Vector2.zero;
        if (ai != null) ai.enabled = false;

        // 弹一下
        if (rb != null)
            rb.AddForce(new Vector2(-enemy.transform.localScale.x * 8f, 2f), ForceMode2D.Impulse);

        yield return new WaitForSeconds(stunDuration);

        // 恢复
        if (ai != null) ai.enabled = true;
    }

    IEnumerator HitStop()
    {
        Time.timeScale = 0f;                            // 暂停
        yield return new WaitForSecondsRealtime(0.05f);  // 停 0.05 秒（用真实时间，不受暂停影响）
        Time.timeScale = 1f;                            // 恢复
    }
}
