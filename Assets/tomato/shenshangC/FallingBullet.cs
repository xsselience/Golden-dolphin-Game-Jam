using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingBullet : MonoBehaviour
{
    [Header("设置")]
    public float fallSpeed = 8f;      // 下落速度
    public int damage = 20;           // 伤害值
    public float targetY = 0f;        // 落到多低算地面

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("黑入自爆")]
    public bool isHacked = false;
    [SerializeField] private float hackSpeed = 10f;
    [SerializeField] private int hackDamage = 30;
    private Transform bossTarget;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        if (isHacked)
        {
            HackRush();
            return;
        }
        // 向下移动
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // 落到地面以下就销毁
        if (transform.position.y <= targetY - 20f)
        {
            Destroy(gameObject);
        }
    }

    private int CalcDamage(int baseDamage, player ps)
    {
        if (ps == null) return baseDamage;

        // 完美弹反：无伤
        if (ps.perfectActive)
        {
            Debug.Log("完美弹反子弹！");
            return 0;
        }

        // 普通格挡：只吃 30%
        if (ps.isBlocking)
        {
            return Mathf.RoundToInt(baseDamage * 0.3f);
        }

        return baseDamage;
    }



    void OnTriggerEnter2D(Collider2D other)
    {
        if (isHacked) return;   // 黑入中不触发原碰撞逻辑

        int layer = 1 << other.gameObject.layer;

        if ((layer & playerLayer) != 0)
        {
            player p = other.GetComponent<player>();
            int finalDamage = CalcDamage(damage, p);

            if (finalDamage > 0 && p != null)
                p.TakeDamage(finalDamage);
            else if (finalDamage == 0 && p != null)
                p.ActivateInvincibility();

            Destroy(gameObject);
        }

        if ((layer & groundLayer) != 0)
        {
            Destroy(gameObject);
        }
    }

    // ==================== 黑入 ====================

    public void GetHacked(Transform boss)
    {
        isHacked = true;
        bossTarget = boss;
    }

    void HackRush()
    {
        if (bossTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        // 朝向 Boss
        Vector2 dir = (bossTarget.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 加速冲
        transform.position = Vector2.MoveTowards(
            transform.position, bossTarget.position, hackSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, bossTarget.position) < 1f)
        {
            bossTarget.GetComponent<boss1ai>()?.TakeDamage(hackDamage);
            bossTarget.GetComponent<boss1ai>()?.Stun(5f);
            Destroy(gameObject);
        }
    }

    public void SetHighlight(bool on)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = on ? Color.cyan : Color.white;
    }

    void OnMouseDown()
    {
        player p = FindObjectOfType<player>();
        if (p != null) p.HackBullet(this);
    }

}
