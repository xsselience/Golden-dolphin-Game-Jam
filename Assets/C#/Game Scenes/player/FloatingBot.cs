using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FloatingBot : MonoBehaviour
{
    [Header("跟随")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 followOffset = new Vector2(1.5f, 1f);
    [SerializeField] private float followSpeed = 4f;

    [Header("漂浮")]
    [SerializeField] private float floatHeight = 0.25f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("拖尾")]
    [SerializeField] private Color trailColor = new Color(0.4f, 0.7f, 1f, 0.6f);
    [SerializeField] private float trailWidth = 0.08f;
    [SerializeField] private float trailTime = 0.3f;

    [Header("手动放置")]
    [SerializeField] private bool ignoreSceneCheck = false;   // 手动放的勾上
    [SerializeField] private Transform customTarget;     // 自选跟随目标

    private Vector3 basePosition;
    private TrailRenderer trailRenderer;

    void Awake()
    {
        if (target == null && customTarget != null)
            target = customTarget;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
            trailRenderer = gameObject.AddComponent<TrailRenderer>();

        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.startColor = trailColor;
        trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = 0f;
        trailRenderer.time = trailTime;
        trailRenderer.minVertexDistance = 0.05f;
        trailRenderer.sortingOrder = -1;

        SceneManager.sceneLoaded += OnSceneLoaded;
        CheckScene();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckScene();
    }

    void CheckScene()
    {
        if (ignoreSceneCheck) return;   // 手动放置的不受场景限制
        int index = SceneManager.GetActiveScene().buildIndex;
        gameObject.SetActive(index == 1 || index == 2);
    }

    void FixedUpdate()
    {
        if (target == null || !gameObject.activeSelf) return;

        Vector3 targetPos = target.position + (Vector3)followOffset;
        targetPos.z = transform.position.z;

        basePosition = Vector3.Lerp(basePosition, targetPos, followSpeed * Time.deltaTime);

        float floatOffset = Mathf.Sin(Time.time * floatSpeed * Mathf.PI * 2f) * floatHeight;
        transform.position = basePosition + new Vector3(0f, floatOffset, 0f);
    }

    public void SetFollowOffset(Vector2 offset) => followOffset = offset;
}
