using UnityEngine;

/// <summary>
/// 传送目标点 —— 场景中放置一个空物体，作为玩家传送到达的位置。
///
/// 用法：
///   创建空物体 → 挂本脚本 → 放到目标房间内的入口位置
///   RoomGate 的 Target Point 拖入此物体引用。
/// </summary>
public class RoomSpawnPoint : MonoBehaviour
{
    [Tooltip("此传送点的唯一标识（调试用，可不填）")]
    public string spawnPointId = "";

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawIcon(transform.position + Vector3.up * 0.2f, "sv_label_3", true);

        string label = "[Spawn]";
        if (!string.IsNullOrEmpty(spawnPointId)) label += " " + spawnPointId;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, label);
    }
#endif
}
