using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DoorPromptText : MonoBehaviour
{
    public static DoorPromptText Instance { get; private set; }
    private Text promptText;

    void Awake()
    {
        Instance = this;
        promptText = GetComponent<Text>();
        if (promptText != null) promptText.enabled = false;
    }

    public void Show(string msg)
    {
        if (promptText != null)
        {
            promptText.text = msg;
            promptText.enabled = true;
        }
    }

    public void Hide()
    {
        if (promptText != null)
            promptText.enabled = false;
    }
}
