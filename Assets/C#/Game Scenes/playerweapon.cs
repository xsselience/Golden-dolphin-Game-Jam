using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerweapon : MonoBehaviour
{
    [Header("伤害数值")]
    [SerializeField] private int damageToEnemy;

    [Header("图层")]
    [SerializeField] private LayerMask enemyLayer;

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    void HandleHit(GameObject hitObject)
    {
        int hitLayer = 1 << hitObject.layer;

        if ((hitLayer & enemyLayer) != 0)
        {
            bossenemy enemyHealth = hitObject.GetComponent<bossenemy>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damageToEnemy);
            }
        }
    }
}
