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
        Debug.Log($"武器命中: {hitObject.name} timeScale={Time.timeScale}");
        int hitLayer = 1 << hitObject.layer;

        Boss2HitPoint hp = hitObject.GetComponent<Boss2HitPoint>();
        if (hp != null) { hp.TakeHit(damageToEnemy); return; }

        if ((hitLayer & enemyLayer) != 0)
        {
            // 近战小兵
            bossenemy smallEnemy = hitObject.GetComponent<bossenemy>();
            if (smallEnemy != null) { smallEnemy.TakeDamage(damageToEnemy); return; }

            // 近战巡逻兵
            enemy normalEnemy = hitObject.GetComponent<enemy>();
            if (normalEnemy != null) { normalEnemy.TakeDamage(damageToEnemy); return; }

            // 远程敌人
            RangedEnemy ranged = hitObject.GetComponent<RangedEnemy>();
            if (ranged != null) { ranged.TakeDamage(damageToEnemy); return; }

            // Boss1
            boss1ai boss1 = hitObject.GetComponent<boss1ai>();
            if (boss1 != null) { boss1.TakeDamage(damageToEnemy); return; }

            // Boss2
            boss2ai boss2 = hitObject.GetComponent<boss2ai>();
            if (boss2 != null) { boss2.TakeDamage(damageToEnemy); }
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
