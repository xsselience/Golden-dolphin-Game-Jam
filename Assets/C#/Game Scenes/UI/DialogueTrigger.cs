using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("对话内容")]
    [SerializeField] private string[] lines;        // "说话人|内容"
    [SerializeField] private bool triggerOnce = true; // 只触发一次

    [Header("图层")]
    [SerializeField] private LayerMask playerLayer;

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && triggered) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        player p = other.GetComponent<player>();
        if (p == null || p.controlsDisabled) return;

        DialogueUI ui = FindObjectOfType<DialogueUI>();
        if (ui == null) return;

        triggered = true;
        ui.PlayDialogue(lines);
    }
}
