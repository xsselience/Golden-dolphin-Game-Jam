using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("结局文本")]
    [SerializeField] private GameObject endingTextPanel;        // 包含 Text 的面板
    [SerializeField] private Text endingText;
    [SerializeField] private string[] endingLines1;             // 算力=0 的文本
    [SerializeField] private string[] endingLines2;             // 算力>0 的文本
    [SerializeField] private float textSpeed = 0.05f;           // 逐字速度
    [SerializeField] private float textStartDelay = 0.5f;       // 动画结束后延迟

    [Header("结局")]
    [SerializeField] private bool isEndingScene = false;   // 勾上就走结局文本，不勾正常切场景

    [Header("结局音效")]
    [SerializeField] private AudioClip endingMusic1;      // 结局1音乐
    [SerializeField] private AudioClip endingMusic2;      // 结局2音乐

    private AudioSource audioSource;
    private int currentEnding = 1;

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
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
        // 停掉场景 BGM
        music bgm = FindObjectOfType<music>();
        if (bgm != null) bgm.StopMusic();

        // 播结局音乐
        AudioClip clip = currentEnding == 1 ? endingMusic1 : endingMusic2;
        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }

        if (isEndingScene)
            StartCoroutine(PlayEndingText());
        else
            DoSwitchScene();
    }

    IEnumerator PlayEndingText()
    {
        Debug.Log("[PlayEndingText] 协程启动");

        yield return new WaitForSecondsRealtime(textStartDelay);
        Debug.Log("[PlayEndingText] 延迟结束");

        int cyberPower = currentPlayer != null ? currentPlayer.GetCyberPower() : 0;
        string[] lines = cyberPower > 0 ? endingLines2 : endingLines1;
        Debug.Log($"[PlayEndingText] cyberPower={cyberPower}, lines数量={lines?.Length ?? 0}");

        if (endingTextPanel != null) endingTextPanel.SetActive(true);
        Debug.Log($"[PlayEndingText] endingTextPanel={(endingTextPanel != null ? "有" : "null")}, endingText={(endingText != null ? "有" : "null")}");

        if (endingText == null) yield break;

        foreach (string line in lines)
        {
            Debug.Log($"[PlayEndingText] 开始打印一行, 长度={line.Length}");
            for (int i = 0; i <= line.Length; i++)
            {
                endingText.text = line.Substring(0, i);
                float start = Time.realtimeSinceStartup;
                yield return new WaitForSecondsRealtime(textSpeed);
                float actual = Time.realtimeSinceStartup - start;
                Debug.Log($"i={i}, expected={textSpeed:F3}s, actual={actual:F3}s, fps={1f / Time.unscaledDeltaTime:F0}");
            }

            while (!Input.anyKeyDown) yield return null;
            yield return null;
        }

        endingText.text += "\n\n—— 点击任意键返回主菜单 ——";
        while (!Input.anyKeyDown) yield return null;

        SceneManager.LoadScene(0);
    }

    void DoSwitchScene()
    {
        GameManager.Instance?.SaveToSlot(-1);
        SceneManager.LoadScene(nextSceneIndex);
    }

    public void TriggerEnding(int ending)
    {
        if (exitTriggered) return;
        exitTriggered = true;
        currentEnding = ending;

        if (playExit && gateAnim != null)
            gateAnim.SetBool("gonextlevel", true);
        else
            OnExitEnd();
    }
}
