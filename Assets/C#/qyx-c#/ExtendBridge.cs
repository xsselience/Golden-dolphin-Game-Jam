using UnityEngine;
using System.Collections;

public class ExtendBridge : MonoBehaviour
{
    [Header("交互提示文字")]
    public string tipText = "按F延伸桥梁";
    [Header("桥基础行走碰撞体")]
    public BoxCollider2D bridgeCol;
    [Header("桥梁整体向前平移距离")]
    public float extendMoveDist = 3.5f;
    [Header("平移全过程耗时（秒）")]
    public float extendDuration = 1.2f;
    [Header("延伸动画控制器(如果有动画才启用）")]
    public Animator bridgeAnim;
    public string animParam = "IsExtend";

    private InteractHintTrigger hintTrigger;
    private bool isFinished = false;
    private float originColSizeX;
    private float originColOffsetX;

    void Start()
    {
        hintTrigger = GetComponent<InteractHintTrigger>();
        hintTrigger.tipText = tipText;

        if (bridgeCol != null)
        {
            originColSizeX = bridgeCol.size.x;
            originColOffsetX = bridgeCol.offset.x;
        }
    }

    void Update()
    {
        if (isFinished) return;
        if (hintTrigger.isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(BridgeMoveCoroutine());
        }
    }

    IEnumerator BridgeMoveCoroutine()
    {
        isFinished = true;
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + new Vector2(extendMoveDist, 0);
        float timePassed = 0f;

        // 播放延伸动画
        if (bridgeAnim != null)
            bridgeAnim.SetBool(animParam, true);

        // 整体平滑向前平移物体
        while (timePassed < extendDuration)
        {
            timePassed += Time.deltaTime;
            float t = timePassed / extendDuration;
            transform.position = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        // 锁定最终位置
        transform.position = targetPos;

        // 同步拉长碰撞体，保留原有区域、向前新增通行区域
        if (bridgeCol != null)
        {
            float finalWidth = originColSizeX + extendMoveDist;
            bridgeCol.size = new Vector2(finalWidth, bridgeCol.size.y);
            bridgeCol.offset = new Vector2(originColOffsetX + extendMoveDist / 2f, bridgeCol.offset.y);
        }

        // 关闭交互提示
        HintManager.Instance.HideHint(gameObject);
        hintTrigger.enabled = false;
    }
}