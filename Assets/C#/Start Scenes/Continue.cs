using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Continue : MonoBehaviour
{
    public GameObject[] groupA; // 三个按钮
    public GameObject[] groupB; // 四个按钮

    // Continue按钮点击 → 显示A，隐藏B
    public void OnContinueClick()
    {
        SetGroup(groupA, true);
        SetGroup(groupB, false);
    }

    // 另一个按钮点击 → 显示B，隐藏A
    public void OnBackClick()
    {
        SetGroup(groupA, false);
        SetGroup(groupB, true);
    }

    void SetGroup(GameObject[] group, bool active)
    {
        foreach (var obj in group)
            obj.SetActive(active);
    }
}
