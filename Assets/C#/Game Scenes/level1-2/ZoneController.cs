using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneController : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] private DamageZone linkedZone;
    [SerializeField] private SpriteRenderer sr;

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

        if (linkedZone != null) linkedZone.Disable();
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
        if (p != null) p.TryActivateZone(this);
    }
}
