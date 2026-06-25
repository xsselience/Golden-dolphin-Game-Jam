using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowZone : MonoBehaviour
{
    private float slowFactor;
    private float duration;
    private player playerScript;

    // 备份原始值
    private float originalSpeed;
    private float originalJumpSpeed;
    private float originalDashSpeed;
    private bool applied = false;

    public void Init(float slowFactor, float duration)
    {
        this.slowFactor = slowFactor;
        this.duration = duration;
        Destroy(gameObject, duration + 0.1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        player p = other.GetComponent<player>();
        if (p != null && !applied)
        {
            playerScript = p;

            originalSpeed = p.speed;
            originalJumpSpeed = p.speedjump;
            originalDashSpeed = p.dashSpeed;

            p.speed *= slowFactor;
            p.speedjump *= slowFactor;
            p.dashSpeed *= slowFactor;

            applied = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        player p = other.GetComponent<player>();
        if (p != null && p == playerScript && applied)
        {
            Restore();
        }
    }

    void OnDestroy()
    {
        Restore();
    }

    void Restore()
    {
        if (playerScript != null && applied)
        {
            playerScript.speed = originalSpeed;
            playerScript.speedjump = originalJumpSpeed;
            playerScript.dashSpeed = originalDashSpeed;
            playerScript = null;
            applied = false;
        }
    }
}
