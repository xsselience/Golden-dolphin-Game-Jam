using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractHintTrigger : MonoBehaviour  //负责碰撞检测、弹出文字
{
    [Header("靠近这个物体弹出的提示文字，例：按F交互 / 按C黑入")]
    public string showText;

    // 玩家走进碰撞范围触发
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 只识别Tag为Player的玩家
        if (other.CompareTag("Player"))
        {
            TipHintUI.Instance.ShowHint(showText);
        }
    }

    // 玩家离开碰撞范围触发
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            TipHintUI.Instance.HideHint();
        }
    }
}
