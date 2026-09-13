using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeechBubble : MonoBehaviour
{
    [Header("角色名（和对话里的名字对应）")]
    [SerializeField] private string characterName;

    [Header("气泡 UI")]
    [SerializeField] private GameObject bubbleObject;   // 拖气泡子物体，不是自己
    [SerializeField] private TMP_Text bubbleText;           // 类型要和你用的 Text 组件一致

    // 注意：没有 Start() 里的 SetActive(false)
    // 初始隐藏靠 Inspector 手动操作

    public string GetName() => characterName;

    public void SetText(string text)
    {
        if (bubbleText != null) bubbleText.text = text;
    }

    public void Show()
    {
        if (bubbleObject != null) bubbleObject.SetActive(true);
    }

    public void Hide()
    {
        if (bubbleObject != null) bubbleObject.SetActive(false);
    }
}
