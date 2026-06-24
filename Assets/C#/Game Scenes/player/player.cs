using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour
{
    [Header("物理组件")]
    private Rigidbody2D playerRb;
    private Transform playertrans;

    [Header("移动使用组件")]
    public float speed;
    public float number;

    [Header("跳跃使用组件")]
    public float speedjump;
    private bool injump;
    private bool isGliding;
    private bool isFalling;
    private bool inground;
    public Transform feet;
    public LayerMask ground;

    [Header("攻击使用组件")]
    public bool attack;

    [Header("冲刺使用组件")]
    public float dashSpeed = 20f;//冲刺时速度
    public float dashTime = 0.3f;
    public float dashCooldown = 1f;

    private bool isDashing;
    private float dashTimer;    //冲刺持续多久
    private float cooldownTimer;//冲刺冷却时间
    private int dashDir;        // 冲刺锁定的方向

    [Header("穿越平台使用组件")]
    public float Stime;//用了限制下落时松开s会被弹开的

    [Header("图层组件")]
    private int playerLayer;
    private int platformLayerIndex;

    [Header("动画使用组件")]
    private Animator anim;

    [Header("生命值使用组件")]
    public int health = 100;

    [Header("无敌")]
    public float invincibilityDuration = 1f;
    private bool isInvincible = false;
    [Header("无敌碰撞忽略")]
    [SerializeField] private LayerMask enemyLayers;  // 把 Enemy 和 Boss 的图层都勾上

    [Header("完美弹反窗口")]
    public float perfectWindow = 0.2f;   // 右键按下后多久是完美弹反

    [Header("普通格挡")]
    public float blockDamageReduction = 0.5f;  // 格挡减免比例 (0.5=减半)

    [Header("击退")]
    public bool isKnockedBack = false;

    [Header("格挡判定")]
    public SpriteRenderer sr;
    public bool isBlocking;         // 右键按住
    public bool perfectActive;      // 还在完美窗口内


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
        Defense();
        FixedupdateCheck();
    }

    private void FixedUpdate()//运动用
    {
        cooldownTimer -= Time.deltaTime;
        move();
    }

    public void move()//移动
    {
        if (isKnockedBack) return;

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
        if (Input.GetButtonDown("Jump") && inground)//跳跃需要
        {
            isGliding = false;
            playerRb.gravityScale = 6;
            playerRb.velocity = new Vector2(playerRb.velocity.x, speedjump);
        }
        if (isFalling == true && Input.GetKey(KeyCode.Space) && !isGliding && !inground)//这个if都是滑翔虽然好像没要求二段跳的但是我还是做了兼容
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

    private void FixedupdateCheck()
    {
        inground = Physics2D.OverlapCircle(feet.position/*获取feet的点*/, .01f/*范围*/, ground/*图层*/);
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

    public void Attacking()//攻击用目前占位但是已做等待动画完善
    {
        if (Input.GetButtonDown("Fire1"))
        {
            attack = true;
        }
    }

    public void AttackEnd()//攻击结束
    {
        attack = false;
    }

    public void Defense()//防御占位
    {
        // 按下右键 → 完美弹反开始
        if (Input.GetMouseButtonDown(1))
        {
            isBlocking = true;
            perfectActive = true;
            StartCoroutine(PerfectWindowTimer());
            sr.color = Color.red;
        }

        // 松开右键 → 取消
        if (Input.GetMouseButtonUp(1))
        {
            isBlocking = false;
            perfectActive = false;
            sr.color = Color.white;
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
        if (isInvincible) return;  // 无敌中，不受伤害

        health -= damage;
        StartCoroutine(InvincibilityRoutine());

        if (health <= 0)
        {
            // 死亡逻辑
        }
    }

    /// <summary>
    /// 触发无敌（完美格挡后由武器/子弹调用）。
    /// </summary>
    public void ActivateInvincibility()
    {
        if (!isInvincible)
            StartCoroutine(InvincibilityRoutine());
    }

    IEnumerator InvincibilityRoutine()//无敌触发
    {
        isInvincible = true;

        // 忽略所有敌方图层的碰撞
        for (int i = 0; i < 32; i++)
        {
            if ((enemyLayers & (1 << i)) != 0)
                Physics2D.IgnoreLayerCollision(playerLayer, i, true);
        }

        yield return new WaitForSeconds(invincibilityDuration);

        // 恢复碰撞
        for (int i = 0; i < 32; i++)
        {
            if ((enemyLayers & (1 << i)) != 0)
                Physics2D.IgnoreLayerCollision(playerLayer, i, false);
        }

        isInvincible = false;
    }

    IEnumerator RestoreAfterTimer()//穿越平台用协程
    {
        yield return new WaitForSeconds(Stime);
        Physics2D.IgnoreLayerCollision(playerLayer, platformLayerIndex, false);
    }

    IEnumerator PerfectWindowTimer()//防御用协程1
    {
        yield return new WaitForSeconds(perfectWindow);
        perfectActive = false;  // 完美窗口关闭，进入普通格挡
    }
}
