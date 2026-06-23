using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerweapon : MonoBehaviour
{
    [Header("伤害数值")]
    [SerializeField] private int damageToEnemy;

    [Header("图层")]
    [SerializeField] private LayerMask enemyLayer;

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("武器 Trigger 碰到: " + other.name);
        HandleHit(other.gameObject);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("武器 Collision 碰到: " + collision.gameObject.name);
        HandleHit(collision.gameObject);
    }

    void HandleHit(GameObject hitObject)
    {
        Debug.Log("武器碰到了: " + hitObject.name + " 图层=" + hitObject.layer);
        int hitLayer = 1 << hitObject.layer;

        if ((hitLayer & enemyLayer) != 0)
        {
            // 普通小兵（bossenemy）
            bossenemy smallEnemy = hitObject.GetComponent<bossenemy>();
            if (smallEnemy != null)
            {
                smallEnemy.TakeDamage(damageToEnemy);
                return;
            }

            // 普通敌人（enemy）
            enemy normalEnemy = hitObject.GetComponent<enemy>();
            if (normalEnemy != null)
            {
                normalEnemy.TakeDamage(damageToEnemy);
                return;
            }

            // Boss
            boss1ai boss = hitObject.GetComponent<boss1ai>();
            if (boss != null)
            {
                boss.TakeDamage(damageToEnemy);
            }
        }
    }
}
