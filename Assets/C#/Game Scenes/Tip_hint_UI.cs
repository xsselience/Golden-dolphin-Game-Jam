using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TipHintUI : MonoBehaviour
{
    // 全局单例，所有物体都能调用它改文字
    public static TipHintUI Instance;
    [Header("这里是屏幕上的提示文字")]
    public Text hintText;

    void Awake()
    {
        Instance = this;
        // 游戏一开始隐藏提示文字
        hintText.gameObject.SetActive(false);
    }

    // 显示提示文字
    public void ShowHint(string textContent)
    {
        hintText.text = textContent;
        hintText.gameObject.SetActive(true);
    }

    // 隐藏提示文字
    public void HideHint()
    {
        hintText.gameObject.SetActive(false);
    }
}