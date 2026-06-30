using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveButton : MonoBehaviour
{
    [SerializeField] private Button targetButton;
    [SerializeField] private string action;   // "Save0" "Save1" "Save2" "Load0" "Load1" "Load2" "Delete0" "Delete1" "Delete2""Save3=auto"

    void Start()
    {
        if (targetButton == null) targetButton = GetComponent<Button>();
        if (targetButton != null)
            targetButton.onClick.AddListener(DoAction);
    }

    void DoAction()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("找不到 GameManager！");
            return;
        }

        switch (action)
        {
            case "Save0": gm.SaveToSlot(0); break;
            case "Save1": gm.SaveToSlot(1); break;
            case "Save2": gm.SaveToSlot(2); break;
            case "Load3": gm.LoadFromSlot(3); break;//auto
            case "Load0": gm.LoadFromSlot(0); break;
            case "Load1": gm.LoadFromSlot(1); break;
            case "Load2": gm.LoadFromSlot(2); break;
            case "Delete0": gm.DeleteSlot(0); break;
            case "Delete1": gm.DeleteSlot(1); break;
            case "Delete2": gm.DeleteSlot(2); break;
        }
    }
}
