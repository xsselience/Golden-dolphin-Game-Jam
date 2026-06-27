using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingBot : MonoBehaviour
{
    [Header("跟随")]
    [SerializeField] private Transform target;          // 玩家
    [SerializeField] private Vector2 followOffset = new Vector2(1.5f, 1f); // 相对玩家的偏移
    [SerializeField] private float followSpeed = 4f;    // 跟随速度（越大越粘）

    [Header("漂浮")]
    [SerializeField] private float floatHeight = 0.25f;  // 起伏幅度
    [SerializeField] private float floatSpeed = 2f;       // 起伏频率

    [Header("拖尾")]
    [SerializeField] private Color trailColor = new Color(0.4f, 0.7f, 1f, 0.6f); // 淡蓝
    [SerializeField] private float trailWidth = 0.08f;
    [SerializeField] private float trailTime = 0.3f;     // 拖尾持续时间

    private Vector3 basePosition;
    private TrailRenderer trailRenderer;

    private void Awake()
    {
        // 自动找玩家
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // 配置 TrailRenderer
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
            trailRenderer = gameObject.AddComponent<TrailRenderer>();

        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.startColor = trailColor;
        trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f); // 尾部淡出
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = 0f;
        trailRenderer.time = trailTime;
        trailRenderer.minVertexDistance = 0.05f;
        trailRenderer.sortingOrder = -1; // 拖尾放在机器人后面
    }

    private void Update()
    {
        
    }

    public void FixedUpdate()
    {
        if (target == null) return;

        // 计算目标位置
        Vector3 targetPos = target.position + (Vector3)followOffset;
        targetPos.z = transform.position.z; // 保持 Z 轴不变

        // 平滑跟随
        basePosition = Vector3.Lerp(basePosition, targetPos, followSpeed * Time.deltaTime);

        // 叠加漂浮
        float floatOffset = Mathf.Sin(Time.time * floatSpeed * Mathf.PI * 2f) * floatHeight;
        transform.position = basePosition + new Vector3(0f, floatOffset, 0f);
    }

    // 方便运行时动态改偏移
    public void SetFollowOffset(Vector2 offset) => followOffset = offset;
}
