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
        HandleHit(other.gameObject);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    void HandleHit(GameObject hitObject)
    {
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
            boss2ai boss2 = hitObject.GetComponent<boss2ai>();
            if (boss2 != null)
            {
                boss2.TakeDamage(damageToEnemy);
                return;
            }
        }
    }

    string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
