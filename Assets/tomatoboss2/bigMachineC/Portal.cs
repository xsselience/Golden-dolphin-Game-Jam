using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Portal : MonoBehaviour
{
    [Header("配对")]
    [SerializeField] private Portal linkedPortal;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color hackHighlight = Color.cyan;
    [SerializeField] private Color activeColor = Color.green;

    [Header("设置")]
    [SerializeField] private LayerMask playerLayer;

    [Header("提示文字")]
    [SerializeField] private string promptText = "[F] 传送";

    private bool isActivated = false;
    private bool playerNearby = false;
    private player currentPlayer;
    private Text promptUI;

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = inactiveColor;

        // 找玩家 Canvas 下的 PromptText
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Transform t = player.transform.Find("Canvas/PromptText");
            if (t != null) promptUI = t.GetComponent<Text>();
        }
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
        if (currentPlayer != null)
        {
            playerNearby = true;
            if (isActivated && promptUI != null)
            {
                promptUI.text = promptText;
                promptUI.enabled = true;
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        player p = other.GetComponent<player>();
        if (p == currentPlayer)
        {
            playerNearby = false;
            currentPlayer = null;
            if (promptUI != null) promptUI.enabled = false;
        }
    }

    public void Activate()
    {
        isActivated = true;
        if (linkedPortal != null)
            linkedPortal.isActivated = true;
        SetBothColor(activeColor);

        // 激活时如果玩家在范围内，显示提示
        if (playerNearby && promptUI != null)
        {
            promptUI.text = promptText;
            promptUI.enabled = true;
        }
    }

    void Teleport()
    {
        if (currentPlayer == null || linkedPortal == null) return;
        if (!currentPlayer.CanTeleport()) return;

        if (promptUI != null) promptUI.enabled = false;

        currentPlayer.transform.position = linkedPortal.transform.position;
        currentPlayer.OnTeleported();
    }

    public void SetForHack(bool on)
    {
        if (isActivated) return;
        if (sr != null) sr.color = on ? hackHighlight : inactiveColor;
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
