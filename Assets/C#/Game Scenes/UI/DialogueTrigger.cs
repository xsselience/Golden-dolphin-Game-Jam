using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("对话内容")]
    [SerializeField] private string[] lines;        // "说话人|内容"
    [SerializeField] private bool triggerOnce = true; // 只触发一次

    [Header("对话ID（留空则用实例记忆，填写则跨场景永久只播一次）")]
    [SerializeField] private string dialogueId;

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;

    private bool triggered = false;
    private static HashSet<string> triggeredIds = new HashSet<string>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && triggered) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        // 如果填了 dialogueId 且已全局触发过，跳过
        if (!string.IsNullOrEmpty(dialogueId) && triggeredIds.Contains(dialogueId)) return;

        player p = other.GetComponent<player>();
        if (p == null || p.controlsDisabled) return;

        DialogueUI ui = FindObjectOfType<DialogueUI>();
        if (ui == null) return;

        ui.PlayDialogue(lines);
        if (ui.IsPlaying())
        {
            triggered = true;
            if (!string.IsNullOrEmpty(dialogueId))
                triggeredIds.Add(dialogueId);
        }
    }
}
