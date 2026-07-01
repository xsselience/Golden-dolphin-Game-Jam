using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DoorSwitch : MonoBehaviour
{
    [Header("门本体")]
    [SerializeField] private Transform doorBody;
    [SerializeField] private Vector3 moveDirection = Vector3.up;
    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float moveSpeed = 2f;

    [Header("检测")]
    [SerializeField] private Vector2 detectionSize = new Vector2(3f, 2f);
    [SerializeField] private LayerMask playerLayer;

    [Header("提示文字")]
    [SerializeField] private string promptText = "[F] 开门";

    private Vector3 doorEndPos;
    private bool playerNearby = false;
    private bool isOpen = false;
    private bool isMoving = false;
    private Text promptUI;

    void Start()
    {
        if (doorBody != null)
            doorEndPos = doorBody.position + moveDirection.normalized * moveDistance;

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
        Vector2 center = (Vector2)transform.position;
        Collider2D hit = Physics2D.OverlapBox(center, detectionSize, 0f, playerLayer);
        bool wasNearby = playerNearby;
        playerNearby = hit != null;

        if (playerNearby && !wasNearby && !isOpen)
        {
            if (promptUI != null)
            {
                promptUI.text = promptText;
                promptUI.enabled = true;
            }
        }
        if (!playerNearby && wasNearby)
        {
            if (promptUI != null) promptUI.enabled = false;
        }

        if (playerNearby && !isOpen && !isMoving && Input.GetKeyDown(KeyCode.F))
            StartCoroutine(OpenDoor());
    }

    IEnumerator OpenDoor()
    {
        isMoving = true;
        if (promptUI != null) promptUI.enabled = false;

        while (doorBody != null && Vector3.Distance(doorBody.position, doorEndPos) > 0.02f)
        {
            doorBody.position = Vector3.MoveTowards(doorBody.position, doorEndPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        if (doorBody != null) doorBody.position = doorEndPos;
        isOpen = true;
        isMoving = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, detectionSize);
    }
}
