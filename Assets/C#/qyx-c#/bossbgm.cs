using UnityEngine;

public class SceneDoubleBGM : MonoBehaviour
{
    [Header("场景默认BGM（进入场景自动播放）")]
    public AudioClip defaultBGM;
    [Header("碰撞区域触发BGM")]
    public AudioClip zoneBGM;
    [Header("音乐音量")]
    [Range(0, 1)] public float volume = 0.6f;

    private AudioSource audioPlayer;
    private bool zoneTriggered = false;

    void Start()
    {
        // 自动添加音频播放器
        audioPlayer = GetComponent<AudioSource>();
        if (audioPlayer == null)
            audioPlayer = gameObject.AddComponent<AudioSource>();

        audioPlayer.volume = volume;
        audioPlayer.loop = true;

        // 进入场景立刻播放默认BGM
        if (defaultBGM != null)
        {
            audioPlayer.clip = defaultBGM;
            audioPlayer.Play();
        }
    }

    // 玩家进入方形碰撞区，切换第二首BGM
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 只触发一次，防止反复切换
        if (zoneTriggered || !other.CompareTag("Player") || zoneBGM == null) return;

        zoneTriggered = true;
        audioPlayer.clip = zoneBGM;
        audioPlayer.Play();
    }

    // 场景视图绘制碰撞框方便调整大小
    void OnDrawGizmosSelected()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, box.size);
        }
    }
}