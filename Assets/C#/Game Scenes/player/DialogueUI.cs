using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DialogueUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text contentText;
    [SerializeField] private float textSpeed = 0.05f;

    private bool isPlaying = false;

    void Start()
    {
        dialoguePanel.SetActive(false);
    }

    /// <summary>播放一段对话。lines 是说话人|内容 的数组。</summary>
    public void PlayDialogue(string[] lines)
    {
        if (isPlaying) return;
        StartCoroutine(DialogueRoutine(lines));
    }

    IEnumerator DialogueRoutine(string[] lines)
    {
        isPlaying = true;
        dialoguePanel.SetActive(true);

        isPlaying = true;
        Time.timeScale = 0f;                      // 暂停游戏
        dialoguePanel.SetActive(true);


        foreach (string line in lines)
        {
            string[] parts = line.Split('|');
            string speaker = parts.Length > 0 ? parts[0] : "";
            string content = parts.Length > 1 ? parts[1] : line;

            if (speakerText != null) speakerText.text = speaker;
            if (contentText != null)
            {
                contentText.text = "";
                foreach (char c in content)
                {
                    contentText.text += c;
                    yield return new WaitForSecondsRealtime(textSpeed);
                }
            }

            // 等玩家按键继续
            while (!Input.anyKeyDown)
                yield return null;
            yield return null; // 防止同一帧跳过下一句
        }

        dialoguePanel.SetActive(false);
        Time.timeScale = 1f;                     // 恢复
        isPlaying = false;
    }

    public bool IsPlaying() => isPlaying;
}
