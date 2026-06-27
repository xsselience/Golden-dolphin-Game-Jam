using UnityEngine;
using UnityEngine.UI;

public class HintManager : MonoBehaviour
{
    // 全局唯一实例，所有交互物体直接调用
    public static HintManager Instance;
    [Header("场景里的提示文字")]
    public Text hintTextUI;

    // 记录当前正在显示提示的物体
    public GameObject currentHintObj;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        hintTextUI.gameObject.SetActive(false);
        currentHintObj = null;
    }

    // 仅当传入物体是当前激活物体时，才隐藏
    public void ShowHint(string text, GameObject sender)
    {
        currentHintObj = sender;
        hintTextUI.text = text;
        hintTextUI.gameObject.SetActive(true);
    }

    public void HideHint(GameObject sender)
    {
        // 只有发起隐藏的物体是当前激活物体，才执行隐藏
        if (currentHintObj == sender)
        {
            hintTextUI.gameObject.SetActive(false);
            currentHintObj = null;
        }
    }
}