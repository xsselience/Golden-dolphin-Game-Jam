using UnityEngine;
using System.Collections;

public class VendingMachine : MonoBehaviour
{
    [Header("交互提示文字")]
    public string tipText = "按F使用售货机";
    [Header("是否为特殊任务售货机")]
    public bool isSpecialVendingMachine = false;
    [Header("两次交互冷却间隔(秒)")]
    public float interactCooldown = 2f;
    [Header("售货机动画控制器（预留）")]
    public Animator vendingAnim;
    public string animParam = "UseMachine";
    [Header("交互按键是F")]
    private const KeyCode interactKey = KeyCode.F;

    // 全局黑入锁定变量
    public static bool Is_Hack_Locked = true;

    private InteractHintTrigger hintTrigger;
    // 冷却计时器
    private float currentCooldownTimer = 0f;
    // 仅特殊售货机使用：记录是否是第一次交互
    private bool hasFirstInteract = false;

    void Start()
    {
        hintTrigger = GetComponent<InteractHintTrigger>();
        hintTrigger.tipText = tipText;
    }

    void Update()
    {
        // 冷却倒计时
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
            return;
        }

        // 玩家在交互圈内按下F触发
        if (hintTrigger.isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(VendingInteractCoroutine());
        }
    }

    IEnumerator VendingInteractCoroutine()
    {
        // 开启冷却，冷却时间内不能再次交互
        currentCooldownTimer = interactCooldown;

        float timePassed = 0f;
        float interactProcessTime = 0.5f; // 交互动作过渡时长

        // 1. 播放售货机使用动画
        if (vendingAnim != null)
            vendingAnim.SetBool(animParam, true);

        // 2. 预留音乐播放占位
        PlaySavePointBGM();

        // 平滑交互等待过程
        while (timePassed < interactProcessTime)
        {
            timePassed += Time.deltaTime;
            yield return null;
        }

        // 3. 售货机重置玩家生命值功能和调整屏幕颜色占位
        ResetPlayerHealth();
        AdjustScreenOverlayColor();

        // 4. 特殊售货机专属分支
        if (isSpecialVendingMachine)
        {
            if (!hasFirstInteract)
            {
                // 第一次交互：弹出任务UI界面（预留）
                ShowQuestUI();
                hasFirstInteract = true;
            }
            else
            {
                // 第二次交互：解除全局黑入锁定
                if (Is_Hack_Locked)
                {
                    Is_Hack_Locked = false;
                }
            }
        }
    }

    #region 预留占位空函数
    /// <summary>播放存档点音乐占位</summary>
    void PlaySavePointBGM()
    {

    }

    /// <summary>重置玩家生命值变量占位</summary>
    void ResetPlayerHealth()
    {

    }

    /// <summary>调整屏幕受击遮罩Overlay颜色占位</summary>
    void AdjustScreenOverlayColor()
    {

    }

    /// <summary>特殊售货机第一次交互弹出任务UI占位</summary>
    void ShowQuestUI()
    {

    }
    #endregion
}
