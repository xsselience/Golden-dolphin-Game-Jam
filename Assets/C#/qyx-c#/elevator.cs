using UnityEngine;
using UnityEngine.UI;

public class ElevatorHack : MonoBehaviour
{
    [Header("��Ҽ�ⷶΧ")]
    public Vector2 detectBox = new Vector2(3, 2);
    public LayerMask playerLayer;
    [Header("ͬ������ŵĿ��ŵ���ͼ")]
    public GameObject doorOpenObj;
    [Header("���ݴ���Ŀ������")]
    public Vector2 teleportTargetPos;

    private SpriteRenderer closeDoorSprite;
    private Text promptText;
    // ������ʾ����
    private readonly string hackTip = "[C] �������";
    private readonly string useTip = "[F] ʹ�õ��ݴ���";

    private bool playerNear = false;
    private bool isHacked = false; // �Ƿ��Ѿ����뿪��

    void Start()
    {
        // ��ȡ����������Ⱦ���
        closeDoorSprite = GetComponent<SpriteRenderer>();
        if (closeDoorSprite != null)
            closeDoorSprite.color = new Color(closeDoorSprite.color.r, closeDoorSprite.color.g, closeDoorSprite.color.b, 1f);
        if (doorOpenObj != null) doorOpenObj.SetActive(false);

        // ƥ����Ĳ㼶 Player/Canvas/PromptText
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Transform textTrans = player.transform.Find("Canvas/PromptText");
            if (textTrans != null)
            {
                promptText = textTrans.GetComponent<Text>();
                promptText.enabled = false;
            }
        }
    }

    void Update()
    {
        Collider2D hit = Physics2D.OverlapBox(transform.position, detectBox, 0, playerLayer);
        bool prevNear = playerNear;
        playerNear = hit != null;
        GameObject playerObj = GameObject.FindWithTag("Player");

        #region ��ʾ�����л��߼�
        if (playerNear && !prevNear && promptText != null)
        {
            // ��û������ʾ������ʾ�����Ѵ򿪣���ʾ������ʾ
            promptText.text = isHacked ? useTip : hackTip;
            promptText.enabled = true;
        }
        if (!playerNear && prevNear && promptText != null)
        {
            promptText.enabled = false;
        }
        // ���վ�ڵ����ϣ���̬ˢ����ʾ���֣����ź��Զ��л�F��ʾ��
        if (playerNear && promptText != null)
        {
            promptText.text = isHacked ? useTip : hackTip;
        }
        #endregion

        #region �����߼�
        // 1. δ����ʱ����C����
        if (playerNear && !isHacked && Input.GetKeyDown(KeyCode.C))
        {
            HackElevator();
        }
        // 2. �Ѿ����ź󣬰�F�������
        if (playerNear && isHacked && Input.GetKeyDown(KeyCode.F) && playerObj != null)
        {
            TeleportPlayer(playerObj);
        }
        #endregion
    }

    // ���뿪���߼���ԭ���߼����䣩
    void HackElevator()
    {
        isHacked = true;
        // ��͸�����ع��ţ����屣��
        Color invisible = closeDoorSprite.color;
        invisible.a = 0;
        closeDoorSprite.color = invisible;
        if (doorOpenObj != null)
            doorOpenObj.SetActive(true);
    }

    // ������ҵ�ָ������
    void TeleportPlayer(GameObject player)
    {
        player.transform.position = teleportTargetPos;
    }

    void OnDrawGizmosSelected()
    {
        // ���ݽ�������
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, detectBox);
        // ����Ŀ����ǣ���ɫС��
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(teleportTargetPos, 0.25f);
    }
}