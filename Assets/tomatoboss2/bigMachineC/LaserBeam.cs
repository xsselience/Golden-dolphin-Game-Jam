using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    [Header("射线参数")]
    [SerializeField] private float maxRange = 30f;
    [SerializeField] private float rotateSpeed;   // 度/秒
    [SerializeField] private int laserDamage = 15;

    [Header("外观")]
    [SerializeField] private float lineWidth = 0.15f;
    [SerializeField] private Color lineColor = new Color(1f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private int orderInLayer = 100;

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask coverLayer;
    [SerializeField] private LayerMask wallLayer;

    private Cover currentCover;
    private LineRenderer lr;
    private Vector3 bossPos;
    private float startAngle;
    private float targetAngle;
    private bool rotateRight;

    public void Init(Vector3 bossPos, bool fromTop, bool rotateRight, float range)
    {
        this.bossPos = bossPos;
        this.rotateRight = rotateRight;
        maxRange = range;

        lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.sortingOrder = orderInLayer;

        transform.position = bossPos;

        // 初始角度：向上=90°，向下=-90°
        startAngle = fromTop ? 90f : -90f;
        targetAngle = startAngle + (rotateRight ? 180f : -180f);

        StartCoroutine(Sweep());
    }

    IEnumerator Sweep()
    {
        float angle = startAngle;
        float totalSweep = Mathf.Abs(targetAngle - startAngle);
        float duration = totalSweep / rotateSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            angle = Mathf.Lerp(startAngle, targetAngle, elapsed / duration);

            DrawAndCheck(angle);
            yield return null;
        }

        Destroy(gameObject);
    }

    void DrawAndCheck(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        int blockMask = coverLayer | wallLayer;
        RaycastHit2D blockHit = Physics2D.Raycast(bossPos, dir, maxRange, blockMask);

        float hitDistance = maxRange;
        Cover hitCover = null;

        if (blockHit.collider != null)
        {
            int hitLayer = 1 << blockHit.collider.gameObject.layer;

            if ((hitLayer & coverLayer) != 0)
            {
                hitCover = blockHit.collider.GetComponent<Cover>();
                if (hitCover != null && hitCover.TryBlock())
                    hitDistance = blockHit.distance;
                else
                    hitCover = null;
            }
            else if ((hitLayer & wallLayer) != 0)
            {
                hitDistance = blockHit.distance;
            }
        }

        // 进出切换
        if (hitCover != currentCover)
        {
            if (currentCover != null)
                currentCover.OnLaserExit();
            currentCover = hitCover;
        }

        // 玩家检测
        RaycastHit2D playerHit = Physics2D.Raycast(bossPos, dir, hitDistance, playerLayer);
        if (playerHit.collider != null)
        {
            player p = playerHit.collider.GetComponent<player>();
            if (p != null) p.TakeDamage(laserDamage);
        }

        // 画线
        lr.SetPosition(0, bossPos);
        lr.SetPosition(1, bossPos + (Vector3)(dir * hitDistance));
    }

    void OnDestroy()
    {
        if (currentCover != null)
        {
            currentCover.OnLaserExit();
            currentCover = null;
        }
    }
}
