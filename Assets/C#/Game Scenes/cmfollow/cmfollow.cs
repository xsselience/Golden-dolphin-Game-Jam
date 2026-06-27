using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cmfollow : MonoBehaviour
{
    public Transform target;
    public float smoothing;
    // Start is called before the first frame update

    void Start()
    {
        FindPlayer();
    }
    void FixedUpdate()
    {
        if (target == null)
        {
            FindPlayer();
            return;
        }
        if (target != null)//镜头跟随代码
        {
            if (transform.position != target.position)
            {
                Vector3 targetPos = target.position;
                targetPos.z = transform.position.z;   // ← 锁死 Z 轴，不跟玩家
                transform.position = Vector3.Lerp(transform.position, targetPos, smoothing);
            }
        }
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) target = p.transform;
    }

}
