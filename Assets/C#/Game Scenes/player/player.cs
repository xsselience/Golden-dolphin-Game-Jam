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
    private float attackGuardTimer = 0f;
    private bool attackLocked = false;

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

    [Header("黑入模式")]
    [SerializeField] private float hackSlowTime = 0.1f;
    [SerializeField] private GameObject hackOverlay;
    [SerializeField] private UnityEngine.UI.Text hackTimerText;      // 黑入倒计时
    [SerializeField] private UnityEngine.UI.Text hackCooldownText;   // 冷却倒计时
    [SerializeField] private float hackMaxDuration = 8f;           // 黑入最多持续多久
    private List<FallingBullet> hackedBullets = new List<FallingBullet>();//搜索导弹

    public bool hackingMode = false;
    private float hackTimer;
    private float hackCooldownTimer;
    private List<bossenemy> hackedTargets = new List<bossenemy>();

    [Header("普通格挡")]
    public float blockDamageReduction = 0.5f;  // 格挡减免比例 (0.5=减半)

    [Header("算力系统")]
    [SerializeField] private int maxCyberPower = 100;
    [SerializeField] private int portalActivationCost = 15;//黑入传送门的
    [SerializeField] private float teleportCooldown = 10f;
    [SerializeField] private UnityEngine.UI.Text cyberPowerText;   // UI 显示

    [Header("算力消耗")]
    [SerializeField] private int coverActivationCost = 3;
    [SerializeField] private int boss2MissileHackCost = 5;//黑导弹的
    [SerializeField] private int trapActivationCost = 3;

    private List<Boss2Missile> hackedBoss2Missiles = new List<Boss2Missile>();

    private int currentCyberPower;
    private int hackCount = 0;
    private float teleportCooldownTimer;
    private bool cyberSystemEnabled = false;

    [Header("击退")]
    public bool isKnockedBack = false;

    [Header("格挡判定")]
    public SpriteRenderer sr;
    public bool isBlocking;         // 右键按住
    public bool perfectActive;      // 还在完美窗口内

    [Header("最后一击")]
    [SerializeField] private float finalBlowKnockbackForce = 30f;
    [SerializeField] private float finalBlowKnockbackDuration = 1f;

    [HideInInspector] public bool controlsDisabled = false;

    [Header("动画 Bool 值")]
    public bool attacktrue;
    public bool dfstrue;
    public bool defensedowntrue;
    public bool jumptrue;
    public bool dashtrue;
    public float runfloat;

    
    public void SetHackCount(int count) => hackCount = count;
    public int GetHackCount() => hackCount;
    public void SetCyberPower(int power) { currentCyberPower = power; UpdateCyberUI(); }
    public int GetCyberPower() => currentCyberPower;

    public void SetCyberEnabled(bool on) => cyberSystemEnabled = on;
    public bool IsCyberEnabled() => cyberSystemEnabled;


    // Start is called before the first frame update
    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playertrans = GetComponent<Transform>();
        playerLayer = LayerMask.NameToLayer("player");
        platformLayerIndex = LayerMask.NameToLayer("platform");
        anim = GetComponent<Animator>();
        currentCyberPower = maxCyberPower;
        UpdateCyberUI();
    }

    // Update is called once per frame
    void Update()//技能等精细输入用
    {
        if (controlsDisabled) return;
        if (teleportCooldownTimer > 0)
            teleportCooldownTimer -= Time.unscaledDeltaTime;
        Hacker();
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
        if (controlsDisabled) return;
        cooldownTimer -= Time.deltaTime;
        move();
    }

    public void move()//移动
    {
        if (isKnockedBack) return;
        if (isKnockedBack || attackLocked) return;
        if (isDashing) return;

        number = Input.GetAxis("Horizontal");
        playerRb.velocity = new Vector2(number * speed, playerRb.velocity.y);

        // 奔跑动画：地面 + 有水平输入 → 用速度绝对值
        if (inground && Mathf.Abs(number) > 0.1f)
            runfloat = Mathf.Abs(number);
        else
            runfloat = 0f;

        if (!attack) File();
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
        isFalling = playerRb.velocity.y < 0;

        if (Input.GetButtonDown("Jump") && inground)
        {
            isGliding = false;
            playerRb.gravityScale = 6;
            playerRb.velocity = new Vector2(playerRb.velocity.x, speedjump);
            jumptrue = true;
        }

        // 上升→跳跃动画，下降→下落动画
        if (playerRb.velocity.y > 0.1f && !inground)
            jumptrue = true;
        else if (playerRb.velocity.y <= 0 && !inground)
        {
            jumptrue = false;
        }

        if (inground)
            jumptrue = false;

        if (isFalling && Input.GetKey(KeyCode.Space) && !isGliding && !inground)
        {
            isGliding = true;
            playerRb.gravityScale = 0.3f;
        }
        else if (!isFalling || !Input.GetKey(KeyCode.Space))
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
        float input = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && cooldownTimer <= 0 && Mathf.Abs(input) > 0.1f)
        {
            isDashing = true;
            dashtrue = true;
            dashTimer = dashTime;
            cooldownTimer = dashCooldown;

            if (Mathf.Abs(input) > 0.1f)
                dashDir = input > 0 ? 1 : -1;
            else
                dashDir = transform.localScale.x > 0 ? 1 : -1;

            playerRb.gravityScale = 0;
        }

        if (isDashing)
        {
            playerRb.velocity = new Vector2(dashDir * dashSpeed, 0);
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0)
            {
                isDashing = false;
                dashtrue = false;
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
        if (hackingMode || controlsDisabled || attackLocked) return;

        if (attackGuardTimer > 0)
        {
            attackGuardTimer -= Time.deltaTime;
            return;
        }

        if (Input.GetButtonDown("Fire1") && !attack && !isDashing)
        {
            attack = true;
            attacktrue = true;
            attackLocked = true;
            playerRb.velocity = new Vector2(0, playerRb.velocity.y);   // 停住水平滑动
            StartCoroutine(AttackUnlock());
        }
    }

    IEnumerator AttackUnlock()
    {
        yield return new WaitForSeconds(0.2f);
        attackLocked = false;
    }

    public void AttackEnd()//攻击结束
    {
        attack = false;
        attacktrue = false;
        controlsDisabled = false;    // 动画结束彻底释放
        attackGuardTimer = 0.1f;
    }

    public void Defense()//防御占位
    {
        if (hackingMode || isDashing) return;

        if (Input.GetMouseButtonDown(1))
        {
            isBlocking = true;
            perfectActive = true;
            dfstrue = true;
            defensedowntrue = false;
            StartCoroutine(PerfectWindowTimer());
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (Input.GetMouseButtonUp(1))
            {
                isBlocking = false;
                perfectActive = false;
                defensedowntrue = true;
                // dfstrue 先不动，等放下动画播完再关
                StartCoroutine(DefenseDownTimer());
            }
        }
    }
    IEnumerator DefenseDownTimer()
    {
        yield return new WaitForSeconds(0.3f);  // 放下动画长度
        dfstrue = false;
        defensedowntrue = false;
    }

    public void Hacker()
    {
        // 冷却倒计时
        if (hackCooldownTimer > 0)
        {
            hackCooldownTimer -= Time.unscaledDeltaTime;

            if (hackCooldownText != null)
            {
                if (hackCooldownTimer > 0)
                    hackCooldownText.text = Mathf.CeilToInt(hackCooldownTimer).ToString() + "s";
                else
                    hackCooldownText.text = "";
            }
        }

        // 按 C 进入
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!hackingMode && hackCooldownTimer <= 0)
                EnterHackMode();
        }

        // ESC 退出
        if (hackingMode && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitHackMode(1f);
        }

        // 黑入中倒计时
        if (hackingMode)
        {
            hackTimer -= Time.unscaledDeltaTime;
            if (hackTimerText != null)
                hackTimerText.text = Mathf.CeilToInt(hackTimer).ToString();

            if (hackTimer <= 0)
                ExitHackMode(1f);
        }
    }

    void EnterHackMode()
    {
        hackingMode = true;
        hackTimer = hackMaxDuration;
        Time.timeScale = hackSlowTime;
        Debug.Log("hackOverlay = " + (hackOverlay != null));
        Time.fixedDeltaTime = 0.02f * Time.timeScale;   // ← 物理也跟着慢

        if (hackTimerText != null) hackTimerText.text = Mathf.CeilToInt(hackTimer).ToString();
        if (hackCooldownText != null) hackCooldownText.text = "";

        if (hackOverlay != null)
        {
            hackOverlay.SetActive(true);
        }

        bossenemy[] enemies = FindObjectsOfType<bossenemy>();
        foreach (bossenemy e in enemies)
        {
            if (!e.isHacked)
            {
                e.SetHighlight(true);
                hackedTargets.Add(e);
            }
        }
        FallingBullet[] bullets = FindObjectsOfType<FallingBullet>();
        foreach (FallingBullet b in bullets)
        {
            if (!b.isHacked)
            {
                b.SetHighlight(true);
                hackedBullets.Add(b);
            }
        }
        // 搜索传送门并高亮
        Portal[] portals = FindObjectsOfType<Portal>();
        foreach (Portal p in portals)
        {
            p.SetForHack(true);
        }

        Cover[] covers = FindObjectsOfType<Cover>();
        foreach (Cover c in covers)
            c.SetForHack(true);
        Boss2Missile[] boss2Missiles = FindObjectsOfType<Boss2Missile>();
        foreach (Boss2Missile m in boss2Missiles)
        {
            if (!m.isHacked)
            {
                m.SetHighlight(true);
                hackedBoss2Missiles.Add(m);
            }
        }
        TrapControl[] traps = FindObjectsOfType<TrapControl>();
        foreach (TrapControl t in traps)
            t.SetForHack(true);
        ZoneController[] zones = FindObjectsOfType<ZoneController>();
        foreach (ZoneController z in zones)
            z.SetForHack(true);
        if (sr != null) sr.color = new Color(0f, 3f, 1f);
    }

    /// <summary>
    /// 由小兵点击或按 E 调用。黑入成功后 5 秒冷却。
    /// </summary>
    public void HackEnemy(bossenemy target)
    {
        if (!hackingMode || target == null || target.isHacked) return;

        boss1ai boss = FindObjectOfType<boss1ai>();
        if (boss != null)
            target.GetHacked(boss.transform);

        IncrementHackCount();
        ExitHackMode(5f);
    }

    public void HackBullet(FallingBullet target)
    {
        if (!hackingMode || target == null || target.isHacked) return;

        boss1ai bossAI = FindObjectOfType<boss1ai>();
        if (bossAI != null)
            target.GetHacked(bossAI.transform);

        IncrementHackCount();
        ExitHackMode(5f);
    }

    void ExitHackMode(float cooldown)
    {
        hackingMode = false;
        hackCooldownTimer = cooldown;
        attackGuardTimer = 0.1f;   // ← 加这行，挡住同一帧的点击
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;   // ← 恢复默认
        if (hackOverlay != null) hackOverlay.SetActive(false);
        if (hackTimerText != null) hackTimerText.text = "";

        foreach (bossenemy e in hackedTargets)
        {
            if (e != null) e.SetHighlight(false);
        }
        hackedTargets.Clear();
        foreach (FallingBullet b in hackedBullets)
        {
            if (b != null) b.SetHighlight(false);
        }
        Portal[] portals = FindObjectsOfType<Portal>();
        foreach (Portal p in portals)
        {
            p.SetForHack(false);
        }
        Cover[] covers = FindObjectsOfType<Cover>();
        foreach (Cover c in covers)
            c.SetForHack(false);
        foreach (Boss2Missile m in hackedBoss2Missiles)
        {
            if (m != null) m.SetHighlight(false);
        }
        TrapControl[] traps = FindObjectsOfType<TrapControl>();
        foreach (TrapControl t in traps)
            t.SetForHack(false);
        ZoneController[] zones = FindObjectsOfType<ZoneController>();
        foreach (ZoneController z in zones)
            z.SetForHack(false);
        hackedBoss2Missiles.Clear();
        hackedBullets.Clear();
        if (sr != null) sr.color = Color.white;
    }

    /// <summary>
    /// Boss 反制调用，强制退出黑入并设冷却。
    /// </summary>
    public void ForceExitHackMode(float cooldown)
    {
        if (!hackingMode) return;
        ExitHackMode(cooldown);
    }

    private void SwitchAnim()//动画判定
    {
        anim.SetBool("attacktrue", attacktrue);
        anim.SetBool("dfstrue", dfstrue);
        anim.SetBool("defensedowntrue", defensedowntrue);
        anim.SetBool("jumptrue", jumptrue);
        anim.SetBool("dashtrue", dashtrue);
        anim.SetFloat("runfloat", runfloat);
        anim.SetBool("grounded", inground);
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;  // 无敌中，不受伤害

        health -= damage;
        StartCoroutine(InvincibilityRoutine());

        if (health <= 0)
        {
            controlsDisabled = true;
            anim.SetBool("die", true);
        }
    }

    public void OnDeathUICallback()
    {
        GameManager.Instance?.OnPlayerDied();
    }

    /// <summary>
    /// 触发无敌（完美格挡后由武器/子弹调用）。
    /// </summary>
    public void ActivateInvincibility()
    {
        if (!isInvincible)
            StartCoroutine(InvincibilityRoutine());
    }

    // ==================== 算力系统 ====================

    /// <summary>boss2 激活时启用算力系统（由 boss2 调用）</summary>
    public void EnableCyberSystem()
    {
        cyberSystemEnabled = true;
        // 最大值减去黑入次数
        currentCyberPower = maxCyberPower - hackCount*2;
        if (currentCyberPower < 0) currentCyberPower = 0;
        UpdateCyberUI();
    }

    /// <summary>每次黑入成功 +1（武器/小兵/飞弹黑入成功后调用）</summary>
    public void IncrementHackCount()
    {
        hackCount++;
    }

    /// <summary>黑入到传送门：扣除算力激活</summary>
    public void TryActivatePortal(Portal portal)
    {
        if (!hackingMode || portal == null) return;

        if (currentCyberPower < portalActivationCost)
        {
            Debug.Log("算力不足！");
            return;
        }

        currentCyberPower -= portalActivationCost;
        portal.Activate();
        UpdateCyberUI();
        Debug.Log("传送门已激活！剩余算力：" + currentCyberPower);

        ExitHackMode(5f);   // ← 加这行
    }

    public void TryActivateTrap(TrapControl trap)
    {
        if (!hackingMode || trap == null) return;

        // 教程陷阱不消耗算力、不加计数
        if (!trap.isTutorial)
        {
            if (!cyberSystemEnabled)
            {
                Debug.Log("算力系统未启用！");
                return;
            }

            if (currentCyberPower < trapActivationCost)
            {
                Debug.Log("算力不足！");
                return;
            }

            currentCyberPower -= trapActivationCost;
            UpdateCyberUI();
            IncrementHackCount();
        }

        trap.Activate();
        ExitHackMode(5f);   // 教程也退出黑入 + 冷却
    }

    public void TryActivateZone(ZoneController zone)
    {
        if (!hackingMode || zone == null) return;

        IncrementHackCount();
        zone.Activate();
        ExitHackMode(5f);
    }
    /// <summary>可否传送（冷却检查）</summary>
    public bool CanTeleport()
    {
        return teleportCooldownTimer <= 0;
    }

    /// <summary>完成传送，启动冷却</summary>
    public void OnTeleported()
    {
        teleportCooldownTimer = teleportCooldown;
    }

    void UpdateCyberUI()
    {
        if (cyberPowerText != null)
            cyberPowerText.text = "算力: " + currentCyberPower + "/" + (maxCyberPower - hackCount);
    }

    public void TryActivateCover(Cover cover)
    {
        if (!hackingMode || cover == null) return;

        if (!cyberSystemEnabled)
        {
            Debug.Log("算力系统未启用！");
            return;
        }

        if (currentCyberPower < coverActivationCost)
        {
            Debug.Log("算力不足！");
            return;
        }

        currentCyberPower -= coverActivationCost;
        cover.Activate();
        UpdateCyberUI();
        Debug.Log("掩体已激活！剩余算力：" + currentCyberPower);

        ExitHackMode(5f);
    }

    public void TryHackBoss2Missile(Boss2Missile missile)
    {
        if (!hackingMode || missile == null || missile.isHacked) return;

        if (!cyberSystemEnabled)
        {
            Debug.Log("算力系统未启用！");
            return;
        }

        if (currentCyberPower < boss2MissileHackCost)
        {
            Debug.Log("算力不足！");
            return;
        }

        currentCyberPower -= boss2MissileHackCost;
        missile.GetHacked();
        UpdateCyberUI();
        Debug.Log("导弹已黑入！剩余算力：" + currentCyberPower);

        ExitHackMode(5f);
    }

    /// <summary>boss2 死亡演出后调用</summary>
    public void DeliverFinalBlow(Transform boss, int ending)
    {
        isKnockedBack = true;

        // 大力击飞玩家（远离 Boss）
        float facing = transform.position.x > boss.position.x ? 1f : -1f;
        Vector2 dir = new Vector2(facing, 0.8f);

        Rigidbody2D prb = playerRb;
        prb.velocity = dir * finalBlowKnockbackForce;

        StartCoroutine(FinalBlowRoutine(boss, ending));
    }

    IEnumerator FinalBlowRoutine(Transform boss, int ending)
    {
        yield return new WaitForSeconds(finalBlowKnockbackDuration);
        isKnockedBack = false;

        // Boss 死亡
        boss2ai b2 = boss.GetComponent<boss2ai>();
        if (b2 != null)
            b2.FinalDeath();

        // 播 Timeline 结局
        PlayEndingTimeline(ending);
    }

    void PlayEndingTimeline(int ending)
    {
        // 去 Hierarchy 里找对应的 Timeline 物体
        string timelineName = ending == 1 ? "Ending1_Timeline" : "Ending2_Timeline";
        Debug.Log("播放结局 " + ending + " → " + timelineName);

        // TODO：用 Timeline 播放
        // GameObject tlObj = GameObject.Find(timelineName);
        // if (tlObj != null) tlObj.GetComponent<PlayableDirector>().Play();
    }

    public int GetCurrentCyberPower() => currentCyberPower;

    IEnumerator InvincibilityRoutine()//无敌触发
    {
        isInvincible = true;

        // 忽略所有敌方图层的碰撞
        for (int i = 0; i < 32; i++)
        {
            if ((enemyLayers & (1 << i)) != 0)
                Physics2D.IgnoreLayerCollision(playerLayer, i, true);
        }

        float endTime = Time.time + invincibilityDuration;
        while (Time.time < endTime)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.08f);
        }

        sr.enabled = true;

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
