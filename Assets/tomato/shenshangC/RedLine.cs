using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedLine : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.5f; // 存在时长

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    /// <summary>设置红线底部 Y 坐标，红线从自身位置拉到目标 Y。</summary>
    public void SetTarget(float targetY)
    {
        float height = transform.position.y - targetY;
        // 拉伸 Y 轴缩放
        transform.localScale = new Vector3(transform.localScale.x, height, 1);
    }
}
