using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("配对")]
    [SerializeField] private Portal linkedPortal;    // 目标传送门
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color hackHighlight = Color.cyan;
    [SerializeField] private Color activeColor = Color.green;

    [Header("设置")]
    [SerializeField] private LayerMask playerLayer;

    private bool isActivated = false;   // 已支付算力激活
    private bool playerNearby = false;
    private player currentPlayer;

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = inactiveColor;
    }

    void Update()
    {
        if (isActivated && playerNearby && Input.GetKeyDown(KeyCode.F))
        {
            Teleport();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        currentPlayer = other.GetComponent<player>();
        if (currentPlayer != null) playerNearby = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        player p = other.GetComponent<player>();
        if (p == currentPlayer)
        {
            playerNearby = false;
            currentPlayer = null;
        }
    }

    /// <summary>支付算力，激活传送门对</summary>
    public void Activate()
    {
        isActivated = true;
        if (linkedPortal != null)
            linkedPortal.isActivated = true;

        SetBothColor(activeColor);
    }

    /// <summary>传送</summary>
    void Teleport()
    {
        if (currentPlayer == null || linkedPortal == null) return;

        if (!currentPlayer.CanTeleport()) return;

        currentPlayer.transform.position = linkedPortal.transform.position;
        currentPlayer.OnTeleported();
    }

    /// <summary>黑入模式下高亮</summary>
    public void SetForHack(bool on)
    {
        if (isActivated) return;  // 已激活不变色
        if (sr != null)
            sr.color = on ? hackHighlight : inactiveColor;
    }

    void SetBothColor(Color c)
    {
        if (sr != null) sr.color = c;
        if (linkedPortal != null && linkedPortal.sr != null)
            linkedPortal.sr.color = c;
    }

    void OnMouseDown()
    {
        if (currentPlayer == null)
            currentPlayer = FindObjectOfType<player>();
        if (currentPlayer != null)
            currentPlayer.TryActivatePortal(this);
    }
}
