using UnityEngine;

/// <summary>
/// 房间传送门 —— 玩家碰到 → 黑屏 → 传送到目标 SpawnPoint → 切换 CameraZone → 亮屏。
///
/// 设置：
///   1. 在房间 A 出口创建空物体 "Gate_ToRoomB"
///   2. 挂 BoxCollider2D(IsTrigger=true) + 本脚本
///   3. Target Zone Id = 目标房间的 CameraZone.zoneId
///   4. Target Spawn Point = 目标房间内的 RoomSpawnPoint 空物体
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomGate : MonoBehaviour
{
    [Header("═══ 传送设置 ═══")]
    [Tooltip("目标 CameraZone 的 zoneId")]
    public string targetZoneId;

    [Tooltip("传送到达点（拖入场景中的 RoomSpawnPoint 物体）")]
    public RoomSpawnPoint targetSpawnPoint;

    [Header("═══ 门方向（调试用） ═══")]
    public GateDirection direction = GateDirection.Right;
    public enum GateDirection { Left, Right, Up, Down }

    void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (string.IsNullOrEmpty(targetZoneId))
        {
            Debug.LogWarning("[RoomGate] " + gameObject.name + " 未设置 targetZoneId！");
            return;
        }
        if (targetSpawnPoint == null)
        {
            Debug.LogWarning("[RoomGate] " + gameObject.name + " 未设置 Target Spawn Point！");
            return;
        }
        if (CameraZoneManager.Instance == null)
        {
            Debug.LogWarning("[RoomGate] CameraZoneManager 不存在！");
            return;
        }

        CameraZoneManager.Instance.TriggerRoomTransition(this);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        if (targetSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetSpawnPoint.transform.position);
            UnityEditor.Handles.Label(
                (transform.position + targetSpawnPoint.transform.position) * 0.5f + Vector3.up * 0.3f,
                gameObject.name + " → " + targetSpawnPoint.gameObject.name
            );
        }
    }
#endif
}
