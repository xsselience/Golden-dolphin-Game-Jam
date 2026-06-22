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

    void Update()
    {
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
        int layer = 1 << other.gameObject.layer;
        player ps = other.GetComponent<player>();

        // 碰到玩家
        // 碰到玩家 → 伤害 + 消失
        if ((layer & playerLayer) != 0)
        {
            player Player = other.GetComponent<player>();
            int finalDamage = CalcDamage(damage, Player);
            if (finalDamage > 0 && Player != null)
            {
                Player.TakeDamage(finalDamage);
                Destroy(gameObject);
            }
            else if (finalDamage == 0 && Player != null)
            {
                Player.ActivateInvincibility();
                Destroy(gameObject);
            }
        }

        // 碰到墙壁/地面 → 消失
        if ((layer & groundLayer) != 0)
        {
            Destroy(gameObject);
        }
    }

}
