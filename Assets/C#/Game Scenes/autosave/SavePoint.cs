using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavePoint : MonoBehaviour
{
    [SerializeField] private int slot = 0;
    [SerializeField] private LayerMask playerLayer;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            GameManager.Instance?.SaveToSlot(slot);
            Debug.Log("自动存档触发");
        }
    }
}
