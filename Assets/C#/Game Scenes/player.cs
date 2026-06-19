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

    [Header("攻击使用组件")]
    public bool attack;

    [Header("冲刺使用组件")]
    public float dashSpeed = 20f;//冲刺时速度
    public float dashTime = 0.3f;
    public float dashCooldown = 1f;

    public bool isDashing;
    public float dashTimer;    //冲刺持续多久
    public float cooldownTimer;//冲刺冷却时间
    public int dashDir;        // 冲刺锁定的方向

    [Header("穿越平台使用组件")]
    public float Stime;//用了限制下落时松开s会被弹开的

    [Header("图层组件")]
    private int playerLayer;
    private int platformLayerIndex;

    [Header("动画使用组件")]
    public Animator anim;

    [Header("生命值使用组件")]
    public int health = 10;


    // Start is called before the first frame update
    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playertrans = GetComponent<Transform>();
        playerLayer = LayerMask.NameToLayer("player");
        platformLayerIndex = LayerMask.NameToLayer("platform");
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()//技能等精细输入用
    {
        Attacking();
        dash();
        JUMP();
        IgnoreLayer();
        SwitchAnim();
    }

    private void FixedUpdate()//运动用
    {
        cooldownTimer -= Time.deltaTime;
        move();
    }

    public void move()//移动
    {
        if (isDashing)
        {
            return;
        }
        number = Input.GetAxis("Horizontal");
        playerRb.velocity = new Vector2(number * speed, playerRb.velocity.y);//移动代码number乘以speed速度是
        if (!attack)
        {
            File();
        }
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

    public void dash()//用于实现冲刺实现后带入移动
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && cooldownTimer <= 0)//按下左shift并且不在冲刺并且冷却为0
        {
            isDashing = true;
            dashTimer = dashTime;
            cooldownTimer = dashCooldown;

            // 冲刺开始时锁方向
            float input = Input.GetAxisRaw("Horizontal");//有点丑陋但是作用是锁定冲刺方向删掉会没法动
            if (Mathf.Abs(input) > 0.1f)                 //怠惰一下先不搞明白了大d老师倾情编写
                dashDir = input > 0 ? 1 : -1;
            else
                dashDir = transform.localScale.x > 0 ? 1 : -1;  // 按朝向

            playerRb.gravityScale = 0;
        }

        if (isDashing)
        {
            playerRb.velocity = new Vector2(dashDir * dashSpeed, 0);  // 锁死方向
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0)
            {
                isDashing = false;
                playerRb.velocity = Vector2.zero;
                playerRb.gravityScale = 3f;
            }
        }
    }

    public void IgnoreLayer()//穿越平台用代码
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            Physics2D.IgnoreLayerCollision(playerLayer, platformLayerIndex, true);
            StartCoroutine(RestoreAfterTimer());//启用携程
        }
    }

    public void Attacking()//攻击用目前占位 计划等待动画一起做
    {
        if (Input.GetButtonDown("Fire1"))
        {
            attack = true;
        }
    }

    public void AttackEnd()
    {
        attack = false;
    }

    public void Defense()//防御占位
    {
        if (Input.GetKey(KeyCode.Mouse1))
        {
            //效果为收到伤害减少但是还没做敌人等一手敌人的变量
        }
    }

    public void Hacker()//黑入占位
    {

    }

    private void SwitchAnim()//动画判定
    {
        anim.SetBool("attacktrue", attack);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // 玩家死亡逻辑
        }
    }

    IEnumerator RestoreAfterTimer()//协程
    {
        yield return new WaitForSeconds(Stime);
        Physics2D.IgnoreLayerCollision(playerLayer, platformLayerIndex, false);
    }
}
