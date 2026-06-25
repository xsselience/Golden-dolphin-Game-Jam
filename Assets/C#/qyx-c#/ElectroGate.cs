using UnityEngine;
using System.Collections;

public class ElectroGate : MonoBehaviour
{
    [Header("交互提示文字")]
    public string tipText = "按F开启电子闸门";
    [Header("未交互时阻挡玩家的实体碰撞体")]
    public BoxCollider2D blockCollider;
    [Header("闸门向上移动距离")]
    public float moveUpDistance = 3f;
    [Header("闸门升起全过程时长（单位：秒）")]
    public float moveDuration = 1f;
    [Header("开门动画组件")]
    public Animator gateAnim;
    public string animParam = "IsOpen";

    private InteractHintTrigger hintTrigger;
    private bool isFinished = false;

    void Start()
    {
        hintTrigger = GetComponent<InteractHintTrigger>();
        hintTrigger.tipText = tipText;
    }

    void Update()
    {
        if (isFinished) return;
        // 玩家在圈内按f触发升起
        if (hintTrigger.isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(GateMoveUpCoroutine());
        }
    }

    // 闸门平滑上升协程
    IEnumerator GateMoveUpCoroutine()
    {
        isFinished = true;
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + new Vector2(0, moveUpDistance);
        float timePassed = 0f;


        // 平滑插值移动
        while (timePassed < moveDuration)
        {
            timePassed += Time.deltaTime;
            float t = timePassed / moveDuration;
            transform.position = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // 移动完成，强制对齐目标位置，关闭碰撞体
        transform.position = targetPos;
        if (blockCollider != null)
            blockCollider.enabled = false;

        // 永久关闭交互提示
        HintManager.Instance.HideHint(gameObject);
        hintTrigger.enabled = false;
    }
}