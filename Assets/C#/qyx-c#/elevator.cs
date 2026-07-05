using UnityEngine;
using UnityEngine.UI;

public class ElevatorHack : MonoBehaviour
{
    [Header("玩家检测范围")]
    public Vector2 detectBox = new Vector2(3, 2);
    public LayerMask playerLayer;
    [Header("同坐标叠放的开门电梯图")]
    public GameObject doorOpenObj;
    [Header("电梯传送目标坐标")]
    public Vector2 teleportTargetPos;

    private SpriteRenderer closeDoorSprite;
    private Text promptText;
    // 两段提示文字
    private readonly string hackTip = "[C] 黑入电梯";
    private readonly string useTip = "[F] 使用电梯传送";

    private bool playerNear = false;
    private bool isHacked = false; // 是否已经黑入开门

    void Start()
    {
        // 获取自身关门渲染组件
        closeDoorSprite = GetComponent<SpriteRenderer>();
        closeDoorSprite.color = new Color(closeDoorSprite.color.r, closeDoorSprite.color.g, closeDoorSprite.color.b, 1f);
        if (doorOpenObj != null) doorOpenObj.SetActive(false);

        // 匹配你的层级 Player/Canvas/PromptText
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Transform textTrans = player.transform.Find("Canvas/PromptText");
            if (textTrans != null)
            {
                promptText = textTrans.GetComponent<Text>();
                promptText.enabled = false;
            }
        }
    }

    void Update()
    {
        Collider2D hit = Physics2D.OverlapBox(transform.position, detectBox, 0, playerLayer);
        bool prevNear = playerNear;
        playerNear = hit != null;
        GameObject playerObj = GameObject.FindWithTag("Player");

        #region 提示文字切换逻辑
        if (playerNear && !prevNear && promptText != null)
        {
            // 门没开：显示黑入提示；门已打开：显示传送提示
            promptText.text = isHacked ? useTip : hackTip;
            promptText.enabled = true;
        }
        if (!playerNear && prevNear && promptText != null)
        {
            promptText.enabled = false;
        }
        // 玩家站在电梯上，动态刷新提示文字（开门后自动切换F提示）
        if (playerNear && promptText != null)
        {
            promptText.text = isHacked ? useTip : hackTip;
        }
        #endregion

        #region 按键逻辑
        // 1. 未黑入时，按C开门
        if (playerNear && !isHacked && Input.GetKeyDown(KeyCode.C))
        {
            HackElevator();
        }
        // 2. 已经开门后，按F传送玩家
        if (playerNear && isHacked && Input.GetKeyDown(KeyCode.F) && playerObj != null)
        {
            TeleportPlayer(playerObj);
        }
        #endregion
    }

    // 黑入开门逻辑（原有逻辑不变）
    void HackElevator()
    {
        isHacked = true;
        // 仅透明隐藏关门，物体保留
        Color invisible = closeDoorSprite.color;
        invisible.a = 0;
        closeDoorSprite.color = invisible;
        if (doorOpenObj != null)
            doorOpenObj.SetActive(true);
    }

    // 传送玩家到指定坐标
    void TeleportPlayer(GameObject player)
    {
        player.transform.position = teleportTargetPos;
    }

    void OnDrawGizmosSelected()
    {
        // 电梯交互检测框
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, detectBox);
        // 传送目标点标记（红色小球）
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(teleportTargetPos, 0.25f);
    }
}