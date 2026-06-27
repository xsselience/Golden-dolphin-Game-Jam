using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
    [Header("槽位")]
    [SerializeField] private int slotIndex;

    [Header("UI 组件")]
    [SerializeField] private Text slotLabel;
    [SerializeField] private Text infoText;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;

    void Start()
    {
        Refresh();

        if (loadButton != null)
            loadButton.onClick.AddListener(() => GameManager.Instance?.LoadFromSlot(slotIndex));

        if (deleteButton != null)
            deleteButton.onClick.AddListener(() =>
            {
                SaveManager.Delete(slotIndex);
                Refresh();
            });
    }

    void OnEnable()
    {
        Refresh();
    }

    void Refresh()
    {

        SaveData data = null;
        try
        {
            data = SaveManager.Load(slotIndex);
        }
        catch
        {
            data = new SaveData();
        }

        if (slotLabel != null)
            slotLabel.text = $"存档 {slotIndex + 1}";

        if (data != null && !data.isEmpty)
        {
            if (infoText != null)
                infoText.text = $"时间: {data.saveTime}\n血量: {data.playerHealth}\n场景: {data.sceneName}";

            if (loadButton != null) loadButton.interactable = true;
            if (deleteButton != null) deleteButton.interactable = true;
        }
        else
        {
            if (infoText != null)
                infoText.text = "空";

            if (loadButton != null) loadButton.interactable = false;
            if (deleteButton != null) deleteButton.interactable = false;
        }
    }
}
