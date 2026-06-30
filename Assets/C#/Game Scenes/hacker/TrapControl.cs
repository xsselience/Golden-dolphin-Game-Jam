using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapControl : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] public FallingTrap linkedTrap;   // 控制的落下物
    [SerializeField] private SpriteRenderer sr;

    [Header("教程")]
    [SerializeField] public bool isTutorial = false;   // 教程陷阱不扣算力


    [Header("颜色")]
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color hackHighlight = Color.cyan;
    [SerializeField] private Color activeColor = Color.green;

    private bool isActivated = false;

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = inactiveColor;
    }

    public void Activate()
    {
        if (isActivated) return;
        isActivated = true;

        if (linkedTrap != null)
            linkedTrap.Activate();

        if (sr != null) sr.color = activeColor;
    }

    public void SetForHack(bool on)
    {
        if (isActivated) return;
        if (sr != null) sr.color = on ? hackHighlight : inactiveColor;
    }

    void OnMouseDown()
    {
        player p = FindObjectOfType<player>();
        if (p != null) p.TryActivateTrap(this);
    }
}
