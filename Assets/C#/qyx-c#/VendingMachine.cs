using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class VendingMachine : MonoBehaviour
{
    [Header("交互提示文字")]
    public string tipText = "按F使用售货机";
    [Header("是否为特殊任务售货机")]
    public bool isSpecialVendingMachine = false;
    [Header("两次交互冷却间隔(秒)")]
    public float interactCooldown = 2f;
    [Header("售货机动画控制器")]
    public Animator vendingAnim;
    public string animParam = "UseMachine";

    [Header("售货机区域音乐")]
    public AudioClip saveBGM;
    [Range(0, 1)] public float maxVolume = 0.6f;
    public float fadeDuration = 0.8f;

    [Header("玩家引用")]
    [SerializeField] private player playerRef;

    // 交互按键
    private const KeyCode interactKey = KeyCode.F;
    // 全局黑入锁定变量
    public static bool Is_Hack_Locked = true;

    private Text promptUI;
    private bool playerInRange = false;
    private AudioSource audioSource;
    private float currentVol;
    // 冷却计时器
    private float currentCooldownTimer = 0f;
    // 仅特殊售货机使用：记录是否是第一次交互
    private bool hasFirstInteract = false;

    void Start()
    {
        // 初始化音频组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = saveBGM;
        audioSource.loop = true;
        audioSource.volume = 0;
        audioSource.Play();
        currentVol = 0;

        // 自动找玩家&PromptText
        if (playerRef == null)
            playerRef = FindObjectOfType<player>();

        if (playerRef != null)
        {
            Transform textTrans = playerRef.transform.Find("Canvas/PromptText");
            if (textTrans != null)
            {
                promptUI = textTrans.GetComponent<Text>();
                promptUI.enabled = false;
            }
        }
    }

    void Update()
    {
        // 音乐淡入淡出逻辑每帧执行
        UpdateBGMFade();

        // 冷却倒计时
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
            return;
        }

        // 控制提示文字显示隐藏
        if (promptUI != null)
        {
            if (playerInRange)
            {
                promptUI.text = tipText;
                promptUI.enabled = true;
            }
            else
            {
                promptUI.enabled = false;
            }
        }

        // 玩家在圈内按F交互
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(VendingInteractCoroutine());
        }
    }

    // 音乐淡入淡出更新
    void UpdateBGMFade()
    {
        if (saveBGM == null) return;
        if (playerInRange)
        {
            currentVol = Mathf.MoveTowards(currentVol, maxVolume, maxVolume / fadeDuration * Time.deltaTime);
        }
        else
        {
            currentVol = Mathf.MoveTowards(currentVol, 0, maxVolume / fadeDuration * Time.deltaTime);
        }
        audioSource.volume = currentVol;
    }

    IEnumerator VendingInteractCoroutine()
    {
        // 开启冷却
        currentCooldownTimer = interactCooldown;
        float timePassed = 0f;
        float interactProcessTime = 0.5f;

        // 播放售货机动画
        if (vendingAnim != null)
            vendingAnim.SetBool(animParam, true);

        // 等待交互过渡时间
        while (timePassed < interactProcessTime)
        {
            timePassed += Time.deltaTime;
            yield return null;
        }

        // ===== 仅这一行回血，直接置100，无其他额外逻辑 =====
        if (playerRef != null)
            playerRef.health = 100;

        AdjustScreenOverlayColor();

        // 特殊售货机逻辑
        if (isSpecialVendingMachine)
        {
            if (!hasFirstInteract)
            {
                ShowQuestUI();
                hasFirstInteract = true;
            }
            else
            {
                if (Is_Hack_Locked)
                    Is_Hack_Locked = false;
            }
        }

        // 关闭动画
        if (vendingAnim != null)
            vendingAnim.SetBool(animParam, false);
    }

    // 玩家进入碰撞框
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    // 玩家离开碰撞框
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    void OnDrawGizmosSelected()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawCube(transform.position, col.size);
        }
    }

    #region 预留空函数（不动）
    void AdjustScreenOverlayColor()
    {

    }

    void ShowQuestUI()
    {

    }
    #endregion
}