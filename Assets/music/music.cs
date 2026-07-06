using UnityEngine;

public class ZoneBGM : MonoBehaviour
{
    [Header("���򱳾�����")]
    public AudioClip bgmClip;
    [Header("���ֻ������� 0~1")]
    [Range(0, 1)] public float maxVolume = 0.6f;
    [Header("���뵭��ʱ��(��)")]
    public float fadeTime = 0.8f;

    private AudioSource audioSource;
    private float currentVolume;
    private int playerInsideCount;

    void Start()
    {
        // �Զ�����AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (bgmClip == null)
        {
            Debug.LogWarning($"ZoneBGM on {gameObject.name}: bgmClip 未赋值，不会播放音乐");
            return;
        }

        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.volume = 0f;
        currentVolume = 0f;
        audioSource.Play();
    }

    void Update()
    {
        // ����������� �� ����
        if (playerInsideCount > 0)
        {
            currentVolume = Mathf.MoveTowards(currentVolume, maxVolume, maxVolume / fadeTime * Time.unscaledDeltaTime);
        }
        // ��Ҳ������� �� ������0
        else
        {
            currentVolume = Mathf.MoveTowards(currentVolume, 0f, maxVolume / fadeTime * Time.unscaledDeltaTime);
        }
        audioSource.volume = currentVolume;
    }

    // ��ҽ�������
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideCount++;
        }
    }

    // ����뿪����
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