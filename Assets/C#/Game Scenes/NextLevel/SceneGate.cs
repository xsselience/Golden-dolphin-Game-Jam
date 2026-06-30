using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneGate : MonoBehaviour
{
    [Header("玩家")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private LayerMask playerLayer;

    [Header("到达动画")]
    [SerializeField] private bool playArrival = true;
    [SerializeField] private float arrivalDelay = 0.3f;

    [Header("退场动画")]
    [SerializeField] private bool playExit = true;
    [SerializeField] private int nextSceneIndex;

    private player currentPlayer;
    private Animator gateAnim;
    private Rigidbody2D playerRb;
    private bool exitTriggered = false;

    void Start()
    {
        gateAnim = GetComponent<Animator>();

        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            currentPlayer = pObj.GetComponent<player>();
            playerRb = pObj.GetComponent<Rigidbody2D>();
            // 重置玩家状态
            currentPlayer.controlsDisabled = true;   // 先锁，到地方再开
            playerRb.velocity = Vector2.zero;

            // 重置动画参数
            Animator pAnim = pObj.GetComponent<Animator>();
            if (pAnim != null)
            {
                pAnim.SetBool("attacktrue", false);
                pAnim.SetBool("dfstrue", false);
                pAnim.SetBool("defensedowntrue", false);
                pAnim.SetBool("jumptrue", false);
                pAnim.SetBool("dashtrue", false);
                pAnim.SetFloat("runfloat", 0f);
            }
        }

        if (playArrival && currentPlayer != null)
            StartCoroutine(ArrivalSequence());
        else if (currentPlayer != null)
            currentPlayer.controlsDisabled = false;   // 不播到达动画就立刻解锁
    }

    IEnumerator ArrivalSequence()
    {
        yield return new WaitForSeconds(arrivalDelay);

        float fallWait = 0f;
        while (playerRb != null && Mathf.Abs(playerRb.velocity.y) > 0.5f && fallWait < 2f)
        {
            fallWait += Time.deltaTime;
            yield return null;
        }

        if (playerSpawnPoint != null && currentPlayer != null)
        {
            if (playerRb != null)
            {
                playerRb.velocity = Vector2.zero;
                playerRb.position = playerSpawnPoint.position;
            }
            else
            {
                currentPlayer.transform.position = playerSpawnPoint.position;
            }
        }

        if (gateAnim != null)
        {
            gateAnim.SetBool("startgame", true);
            // 兜底：2 秒后强制解锁（防止动画事件漏绑）
            StartCoroutine(ArrivalTimeout());
        }
        else
        {
            // 没 Animator 直接解锁
            if (currentPlayer != null)
                currentPlayer.controlsDisabled = false;
        }

    }

    IEnumerator ArrivalTimeout()
    {
        yield return new WaitForSeconds(0.3f);
        if (currentPlayer != null && currentPlayer.controlsDisabled)
        {
            currentPlayer.controlsDisabled = false;
            if (gateAnim != null) gateAnim.SetBool("startgame", false);
            Debug.LogWarning("到达动画超时，强制解锁");
        }
    }

    /// <summary>到达动画末端 Animation Event 调用</summary>
    public void OnArrivalEnd()
    {
        if (gateAnim != null)
            gateAnim.SetBool("startgame", false);
        if (currentPlayer != null)
            currentPlayer.controlsDisabled = false;
    }

    public void TriggerExit(player p)
    {
        if (exitTriggered) return;
        exitTriggered = true;

        currentPlayer = p;
        p.controlsDisabled = true;

        // 清除残留动画
        Animator pAnim = p.GetComponent<Animator>();
        if (pAnim != null)
        {
            pAnim.SetBool("attacktrue", false);
            pAnim.SetBool("dashtrue", false);
            pAnim.SetBool("jumptrue", false);
            pAnim.SetBool("dfstrue", false);
            pAnim.SetBool("defensedowntrue", false);
            pAnim.SetFloat("runfloat", 0f);
        }

        if (playExit && gateAnim != null)
        {
            gateAnim.SetBool("gonextlevel", true);
        }
        else
        {
            DoSwitchScene();
        }
    }

    /// <summary>退场动画末端 Animation Event 调用</summary>
    public void OnExitEnd()
    {
        DoSwitchScene();
    }

    void DoSwitchScene()
    {
        GameManager.Instance?.SaveToSlot(-1);
        SceneManager.LoadScene(nextSceneIndex);
    }
}
