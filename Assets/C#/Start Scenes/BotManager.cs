using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BotManager : MonoBehaviour
{
    public static BotManager Instance { get; private set; }

    [Header("机器人预制体")]
    [SerializeField] private GameObject botPrefab;

    [Header("生成偏移")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(1.5f, 1f);

    [Header("等玩家出现的延迟")]
    [SerializeField] private float spawnDelay = 0.05f; // 给玩家 Awake 一点时间

    /// <summary>
    /// 存档数据：是否拥有机器人。读档后设为 true 即可触发生成。
    /// </summary>
    [HideInInspector] public bool hasBot = true;

    private GameObject currentBot;

    private void Awake()
    {
        // 单例 + 跨场景保留
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 监听场景加载
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasBot || botPrefab == null) return;

        // 先销毁旧机器人的引用（旧场景的已被 Unity 自动清理）
        currentBot = null;

        // 延迟等玩家生成
        Invoke(nameof(SpawnBot), spawnDelay);
    }

    private void SpawnBot()
    {
        if (currentBot != null) return; // 已经有了

        Transform player = FindPlayer();
        if (player == null)
        {
            Debug.LogWarning("BotManager：找不到玩家，0.3 秒后重试");
            Invoke(nameof(SpawnBot), 0.3f);
            return;
        }

        Vector3 pos = player.position + (Vector3)spawnOffset;
        pos.z = 0f;
        currentBot = Instantiate(botPrefab, pos, Quaternion.identity);
    }

    private Transform FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.transform : null;
    }

    /// <summary>
    /// 读档时调用：设置状态 + 如果在场景中则立刻生成
    /// </summary>
    public void OnLoadGame(bool savedHasBot)
    {
        hasBot = savedHasBot;
        if (hasBot && currentBot == null)
            SpawnBot();
    }

    /// <summary>
    /// 获得/失去机器人（比如剧情里被摧毁）
    /// </summary>
    public void SetBotActive(bool active)
    {
        hasBot = active;
        if (!active && currentBot != null)
        {
            Destroy(currentBot);
            currentBot = null;
        }
        if (active && currentBot == null)
            SpawnBot();
    }
}
