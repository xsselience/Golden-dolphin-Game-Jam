using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss2HitPoint : MonoBehaviour
{
    [SerializeField] private boss2ai boss;
    [SerializeField] private int hitPointIndex;   // 0, 1, 2

    void Start()
    {
        if (boss == null)
            boss = FindObjectOfType<boss2ai>();

        if (boss != null)
            boss.RegisterHitPoint(this);
    }

    /// <summary>玩家武器调用</summary>
    public void TakeHit(int damage)
    {
        if (boss != null)
            boss.TakeDamage(damage);
    }

    public void SetActive(bool on)
    {
        gameObject.SetActive(on);
    }

    public int GetIndex() => hitPointIndex;
}
