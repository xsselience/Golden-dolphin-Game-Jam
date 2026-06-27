using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cover : MonoBehaviour
{
    [Header("状态")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color hackHighlight = Color.cyan;
    [SerializeField] private Color activeColor = Color.green;

    private bool isActivated = false;
    private bool isBlocking = false;

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = inactiveColor;
    }

    public void Activate()
    {
        isActivated = true;
        isBlocking = false;
        if (sr != null) sr.color = activeColor;
    }

    /// <summary>激光正在碰到我，返回是否成功阻挡</summary>
    public bool TryBlock()
    {
        if (!isActivated) return false;
        isBlocking = true;
        return true;
    }

    /// <summary>激光离开我</summary>
    public void OnLaserExit()
    {
        if (isBlocking)
        {
            isBlocking = false;
            isActivated = false;
            if (sr != null) sr.color = inactiveColor;
        }
    }

    public void SetForHack(bool on)
    {
        if (isActivated) return;
        if (sr != null) sr.color = on ? hackHighlight : inactiveColor;
    }

    void OnMouseDown()
    {
        player p = FindObjectOfType<player>();
        if (p != null) p.TryActivateCover(this);
    }
}
