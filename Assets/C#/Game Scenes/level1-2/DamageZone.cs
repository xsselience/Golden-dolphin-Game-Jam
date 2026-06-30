using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Header("伤害")]
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float tickInterval = 0.3f;

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;

    private bool isActive = true;
    private float tickTimer;
    private player currentPlayer;

    void Update()
    {
        if (!isActive || currentPlayer == null) return;

        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0)
        {
            tickTimer = tickInterval;
            currentPlayer.TakeDamage(damagePerTick);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            currentPlayer = other.GetComponent<player>();
            tickTimer = 0f; // 一进入就扣
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            player p = other.GetComponent<player>();
            if (p == currentPlayer) currentPlayer = null;
        }
    }

    public void Disable()
    {
        isActive = false;
        currentPlayer = null;
    }
}
