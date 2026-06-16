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
    public bool isGliding;
    public bool isFalling;

    // Start is called before the first frame update
    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playertrans = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()//技能等精细输入用
    {
        JUMP();
    }

    private void FixedUpdate()//运动用
    {
        move();
    }

    public void move()//移动
    {
        number = Input.GetAxis("Horizontal");
        playerRb.velocity = new Vector2(number * speed, playerRb.velocity.y);//移动代码number乘以speed速度是
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

    private void JUMP()//跳跃
    {
        isFalling = playerRb.velocity.y < 0;//y轴变化小于0时触发
        if (Input.GetButtonDown("Jump"))//跳跃需要
        {
            isGliding = false;
            playerRb.gravityScale = 6;
            playerRb.velocity = new Vector2(playerRb.velocity.x, speedjump);
        }
        if (isFalling == true && Input.GetKey(KeyCode.Space) && !isGliding)//这个if都是滑翔虽然好像没要求二段跳的但是我还是做了兼容
        {
            isGliding = true;
            playerRb.gravityScale = 0.5f;
        }
        else if(isFalling == false || !Input.GetKey(KeyCode.Space))
        {
            isGliding = false;
            playerRb.gravityScale = 6;
        }
    }
}
