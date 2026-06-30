using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelExit : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private SceneGate sceneGate;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        player p = other.GetComponent<player>();
        if (p != null && sceneGate != null)
            sceneGate.TriggerExit(p);
    }
}
