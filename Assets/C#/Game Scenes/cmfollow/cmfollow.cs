using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cmfollow : MonoBehaviour
{
    public Transform target;
    public float smoothing;
    // Start is called before the first frame update
    void FixedUpdate()
    {
        if (target != null)//镜头跟随代码
        {
            if (transform.position != target.position)
            {
                Vector3 targetPos = target.position;
                transform.position = Vector3.Lerp(transform.position, targetPos, smoothing);
            }
        }
    }

}
