using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss2HitPoint : MonoBehaviour
{
    [SerializeField] private boss2ai boss;
    [SerializeField] private int hitPointIndex;
    [SerializeField] private SpriteRenderer sr;

    void Start()
    {
        if (boss == null) boss = FindObjectOfType<boss2ai>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (boss != null) boss.RegisterHitPoint(this);
    }

    public void TakeHit(int damage)
    {
        if (boss != null) boss.TakeDamage(damage);
    }

    public void Dim()
    {
        if (sr != null)
            sr.color = new Color(0.43f, 0.43f, 0.43f, 1f);   // RGB 110
    }

    public int GetIndex() => hitPointIndex;
}
