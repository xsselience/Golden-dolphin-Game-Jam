using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// 自动炮塔单脚本
/// 1. 扇形弧形视野检测（距离、角度可在面板调节）
/// 2. 预留开火逻辑标记位置，未实现发射子弹
/// 3. 用InteractHintTrigger函数的Trigger圈作为C键交互范围，目前按C后直接销毁物体模拟自爆

public class AutoTurret : MonoBehaviour
{
    [Header(" 扇形视野检测参数 ")]
    [Tooltip("视野最远检测距离")]
    public float sightDistance = 4f;
    [Tooltip("扇形视野左右总角度，如60=左右各30°")]
    public float sightAngle = 60f;
    [Tooltip("玩家所在层级，只检测Player层")]
    public LayerMask playerLayer;

    [Header(" 交互自爆设置 ")]
    [Tooltip("黑入成功后销毁物体的延迟时间")]
    public float destroyDelay = 0.8f;

    // 缓存玩家物体
    private Transform playerTrans;
    // 标记炮塔是否已经被黑入自爆
    private bool isHacked = false;
    // 标记玩家是否处于交互触发圈内
    private bool playerInInteractRange = false;

    void Start()
    {
        // 启动时查找玩家，Tag为Player
        playerTrans = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        // 已经黑入，停止所有逻辑
        if (isHacked) return;

        // 1. 每帧执行扇形视野检测
        CheckSightView();

        // 2. 玩家在交互圈内、按下C键，执行黑入自爆
        if (playerInInteractRange && Input.GetKeyDown(KeyCode.C))
        {
            HackAndDestroy();
        }
    }

    
    /// 扇形弧形视野检测
    void CheckSightView()
    {
        // 1. 计算炮塔指向玩家的方向向量
        Vector2 dirToPlayer = playerTrans.position - transform.position;
        float distanceToPlayer = dirToPlayer.magnitude;

        // 2. 距离超过设定视野，直接返回，不继续判断
        if (distanceToPlayer > sightDistance)
            return;

        // 3. 计算玩家与炮塔正前方的夹角
        float angleBetween = Vector2.Angle(transform.right, dirToPlayer);
        // 夹角小于设定扇形半角，代表玩家在视野内
        if (angleBetween < sightAngle / 2)
        {
            // ========== 预留开火逻辑位置，未实现子弹发射==========
            // FireBullet();
        }
    }

    /// 【预留】开火函数，当前仅占位
    void FireBullet()
    {
        
    }

    
    /// 黑入炮塔，延迟销毁模拟自爆
   
    void HackAndDestroy()
    {
        isHacked = true;
        Invoke(nameof(SelfDestroy), destroyDelay);
    }

    /// 销毁炮塔物体
   
    void SelfDestroy()
    {
        Destroy(gameObject);
    }

   
    // 由InteractHintTrigger的碰撞触发调用，玩家走进交互圈
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInInteractRange = true;
        }
    }
    // 玩家离开交互圈，取消C键响应
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInInteractRange = false;
        }
    }

    

    /// 扇形视野+交互圆形范围（未完成）
    void OnDrawGizmos()
    {
        // 绘制黄色交互圈
        Gizmos.color = Color.yellow;
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            Gizmos.DrawWireSphere(transform.position, col.radius);
        }

        // 2D横版扇形视野，向右(transform.right)展开
        Gizmos.color = Color.red;
        Vector2 forwardDir = transform.right;
        // 2D旋转固定轴 Vector3.back
        Vector2 leftEdge = Quaternion.AngleAxis(-sightAngle / 2, Vector3.back) * forwardDir;
        Vector2 rightEdge = Quaternion.AngleAxis(sightAngle / 2, Vector3.back) * forwardDir;

        Gizmos.DrawLine(transform.position, transform.position + (Vector3)leftEdge * sightDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightEdge * sightDistance);

        // 绘制圆弧
        float step = 5f;
        for (float angle = -sightAngle / 2; angle <= sightAngle / 2; angle += step)
        {
            Vector2 curr = Quaternion.AngleAxis(angle, Vector3.back) * forwardDir;
            Vector2 nextP = Quaternion.AngleAxis(angle + step, Vector3.back) * forwardDir;
            Gizmos.DrawLine(
                transform.position + (Vector3)curr * sightDistance,
                transform.position + (Vector3)nextP * sightDistance
            );
        }
    }
}
