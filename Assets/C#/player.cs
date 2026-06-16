using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour
{
    [Header("物理组件")]
    public Rigidbody2D playerRb;
    public Transform playertrans;

    [Header("移动使用组件")]
    public float speed;
    public float number;

    [Header("跳跃使用组件")]
    public float speedjump;
    public bool injump;
    // Start is called before the first frame update
    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playertrans = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        JUMP();
    }

    private void FixedUpdate()
    {
        move();
    }

    public void move()//移动
    {
        number = Input.GetAxis("Horizontal");
        playerRb.velocity = new Vector2(number * speed, playerRb.velocity.y);//移动代码number乘以speed速度是
        //player.velocitx = new Vector2(number * speed, player.velocitx.x);
        File();
    }

    private void File()//镜像反转
    {
        if (playerRb.velocity.x > .1f)
        {
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        if (playerRb.velocity.x < -.1f)
        {
            transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
    }

    private void JUMP()
    {
        if (Input.GetButtonDown("Jump"))
        {
            playerRb.velocity = new Vector2(playerRb.velocity.x, speedjump);
        }
    }
}
