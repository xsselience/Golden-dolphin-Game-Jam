using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapControl : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] public FallingTrap linkedTrap;   // 控制的落下物
    [SerializeField] private SpriteRenderer sr;

    [Header("教程")]
    [SerializeField] public bool isTutorial = false;   // 教程陷阱不扣算力


    [Header("颜色")]
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color hackHighlight = Color.cyan;
    [SerializeField] private Color activeColor = Color.green;

    private bool isActivated = false;

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = inactiveColor;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null)
            {
                Debug.Log($"点击到了: {hit.gameObject.name}");
            }
            else
            {
                Debug.Log("点击空白区域");
            }
        }
    }


    public void Activate()
    {
        if (isActivated) return;
        isActivated = true;

        if (linkedTrap != null)
            linkedTrap.Activate();

        if (sr != null) sr.color = activeColor;
    }

    public void SetForHack(bool on)
    {
        if (isActivated) return;
        if (sr != null) sr.color = on ? hackHighlight : inactiveColor;
    }

    void OnMouseDown()
    {
        Debug.Log($"[TrapControl] 点击 {gameObject.name}, isTutorial={isTutorial}");
        player p = FindObjectOfType<player>();
        if (p != null) p.TryActivateTrap(this);
    }
}
