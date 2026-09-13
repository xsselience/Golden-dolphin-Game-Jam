using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("逐字速度")]
    [SerializeField] private float textSpeed = 0.03f;

    private bool isPlaying = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayDialogue(string[] lines)
    {
        if (isPlaying || lines == null || lines.Length == 0) return;
        StartCoroutine(DialogueRoutine(lines));
    }

    public bool IsPlaying() => isPlaying;

    IEnumerator DialogueRoutine(string[] lines)
    {
        isPlaying = true;

        foreach (string line in lines)
        {
            // ===== 诊断 =====
            Debug.Log($"原始对话: [{line}]");
            string[] parts = line.Split('|');
            Debug.Log($"分割段数: {parts.Length}");
            // =================

            string speakerName = parts[0].Trim();
            string content = parts.Length > 1 ? parts[1] : "";
            Debug.Log($"解析说话人: [{speakerName}]");

            SpeechBubble bubble = FindBubble(speakerName);
            if (bubble == null)
            {
                Debug.LogWarning($"找不到: [{speakerName}]");
                continue;
            }

            // 只显示当前说话者的气泡
            HideAllBubbles();
            bubble.Show();

            // 逐字显示
            for (int i = 0; i < content.Length; i++)
            {
                bubble.SetText(content.Substring(0, i + 1));
                yield return new WaitForSeconds(textSpeed);
            }

            // 点击继续
            while (!Input.GetMouseButtonDown(0))
                yield return null;
            yield return null;  // 防止同一帧跳过下一句
        }

        HideAllBubbles();
        isPlaying = false;
    }

    SpeechBubble FindBubble(string name)
    {
        SpeechBubble[] all = FindObjectsOfType<SpeechBubble>(true);
        Debug.Log($"场景气泡数量: {all.Length}");

        foreach (SpeechBubble b in FindObjectsOfType<SpeechBubble>())
        {
            if (b.GetName() == name)
                return b;
        }
        return null;
    }

    void HideAllBubbles()
    {
        foreach (SpeechBubble b in FindObjectsOfType<SpeechBubble>())
            b.Hide();
    }
}
