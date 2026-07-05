using UnityEngine;

public class ZoneBGM : MonoBehaviour
{
    [Header("区域背景音乐")]
    public AudioClip bgmClip;
    [Header("音乐基础音量 0~1")]
    [Range(0, 1)] public float maxVolume = 0.6f;
    [Header("淡入淡出时长(秒)")]
    public float fadeTime = 0.8f;

    private AudioSource audioSource;
    private float currentVolume;
    private int playerInsideCount;

    void Start()
    {
        // 自动添加AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.volume = 0f;
        currentVolume = 0f;
        audioSource.Play();
    }

    void Update()
    {
        // 玩家在区域内 → 淡入
        if (playerInsideCount > 0)
        {
            currentVolume = Mathf.MoveTowards(currentVolume, maxVolume, maxVolume / fadeTime * Time.deltaTime);
        }
        // 玩家不在区域 → 淡出到0
        else
        {
            currentVolume = Mathf.MoveTowards(currentVolume, 0f, maxVolume / fadeTime * Time.deltaTime);
        }
        audioSource.volume = currentVolume;
    }

    // 玩家进入区域
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideCount++;
        }
    }

    // 玩家离开区域
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideCount--;
            if (playerInsideCount < 0) playerInsideCount = 0;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(transform.position, col.size);
        }
    }
}