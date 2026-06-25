using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    [Header("射线参数")]
    [SerializeField] private float maxRange = 50f;
    [SerializeField] private float sweepSpeed = 8f;
    [SerializeField] private int laserDamage = 15;

    [Header("LineRenderer 外观")]
    [SerializeField] private float lineWidth = 0.15f;
    [SerializeField] private Color lineColor = new Color(1f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private int orderInLayer = 100;

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask coverLayer;
    [SerializeField] private LayerMask wallLayer;

    private LineRenderer lr;
    private bool fromTop;
    private float topY, bottomY;
    private Vector3 origin;
    private Cover currentCover;

    public void Init(bool fromTop, float topY, float bottomY, Vector3 startPoint)
    {
        // 清除可能残留的 Collider2D
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) Destroy(col);
        this.fromTop = fromTop;
        this.topY = topY;
        this.bottomY = bottomY;

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

        origin = new Vector3(startPoint.x, fromTop ? topY : bottomY, 0);
        transform.position = origin;

        StartCoroutine(Sweep());
    }

    IEnumerator Sweep()
    {
        float targetY = fromTop ? bottomY : topY;
        float startY = origin.y;
        float duration = Mathf.Abs(targetY - startY) / sweepSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentY = Mathf.Lerp(startY, targetY, elapsed / duration);
            Vector3 currentPos = new Vector3(origin.x, currentY, 0);

            DrawAndCheck(currentPos);
            yield return null;
        }

        Destroy(gameObject);
    }

    void DrawAndCheck(Vector3 beamStart)
    {
        Vector2 dir = Vector2.left;

        // 找最近的阻挡物（掩体或墙壁）
        int blockMask = coverLayer | wallLayer;
        RaycastHit2D blockHit = Physics2D.Raycast(beamStart, dir, maxRange, blockMask);

        float hitDistance = maxRange;
        Cover hitCover = null;

        if (blockHit.collider != null)
        {
            int hitLayer = 1 << blockHit.collider.gameObject.layer;

            if ((hitLayer & coverLayer) != 0)
            {
                hitCover = blockHit.collider.GetComponent<Cover>();
                // 掩体：只有激活才阻挡，不激活光线穿过
                if (hitCover == null || !hitCover.TryBlock())
                    hitCover = null;  // 没激活就当作透明
                else
                    hitDistance = blockHit.distance;
            }
            else if ((hitLayer & wallLayer) != 0)
            {
                // 墙壁：纯阻挡，光线截断，不销毁
                hitDistance = blockHit.distance;
            }
        }

        // 掩体进出
        if (hitCover != currentCover)
        {
            if (currentCover != null) currentCover.OnLaserExit();
            currentCover = hitCover;
        }

        // 玩家检测
        RaycastHit2D playerHit = Physics2D.Raycast(beamStart, dir, hitDistance, playerLayer);
        if (playerHit.collider != null)
        {
            player p = playerHit.collider.GetComponent<player>();
            if (p != null) p.TakeDamage(laserDamage);
        }

        // 画线
        lr.SetPosition(0, beamStart);
        lr.SetPosition(1, beamStart + (Vector3)(dir * hitDistance));
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
