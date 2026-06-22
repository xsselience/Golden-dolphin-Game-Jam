using UnityEngine;

public class InteractHintTrigger : MonoBehaviour
{
    [Tooltip("玩家靠近时弹出的提示文本")]
    public string tipText;
    public bool isPlayerInRange = false; //公共变量用于调用

    void Update()
    {
        if (isPlayerInRange)
        {
            HintManager.Instance.ShowHint(tipText, gameObject);
        }
        else
        {
            HintManager.Instance.HideHint(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerInRange = false;
    }
}