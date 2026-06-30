using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public int damage = 10;

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask wallLayer;

    void Start()
    {
        Destroy(gameObject, 5f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
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

        if ((layer & wallLayer) != 0)
        {
            Destroy(gameObject);
        }
    }

    private int CalcDamage(int baseDamage, player p)
    {
        if (p == null) return baseDamage;
        if (p.perfectActive) return 0;
        if (p.isBlocking) return Mathf.RoundToInt(baseDamage * 0.3f);
        return baseDamage;
    }
}
