using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss2Missile : MonoBehaviour
{
    [Header("设置")]
    public float fallSpeed = 8f;
    public int damage = 25;

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("黑入自爆")]
    public bool isHacked = false;
    [SerializeField] private float hackSpeed = 15f;
    [SerializeField] private int hackDamage = 30;

    private Transform bossTarget;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // 自动找 boss2
        boss2ai boss = FindObjectOfType<boss2ai>();
        if (boss != null) bossTarget = boss.transform;
    }

    void Update()
    {
        if (isHacked)
        {
            HackRush();
            return;
        }

        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        if (transform.position.y <= -50f)
            Destroy(gameObject);
    }

    void HackRush()
    {
        if (bossTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 dir = (bossTarget.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        transform.position = Vector2.MoveTowards(
            transform.position, bossTarget.position, hackSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, bossTarget.position) < 1f)
        {
            boss2ai bossAI = bossTarget.GetComponent<boss2ai>();
            if (bossAI != null)
            {
                bossAI.TakeDamage(hackDamage);
                bossAI.InstantlyBreakPoise();
            }
            Destroy(gameObject);
        }
    }

    public void GetHacked()
    {
        isHacked = true;
    }

    public void SetHighlight(bool on)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = on ? Color.cyan : Color.white;
    }

    void OnMouseDown()
    {
        player p = FindObjectOfType<player>();
        if (p != null) p.TryHackBoss2Missile(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isHacked) return;

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

    private int CalcDamage(int baseDamage, player p)
    {
        if (p == null) return baseDamage;
        if (p.perfectActive) return 0;
        if (p.isBlocking) return Mathf.RoundToInt(baseDamage * 0.3f);
        return baseDamage;
    }
}
