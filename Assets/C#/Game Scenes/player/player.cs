using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour
{
    [Header("物理组件")]
    private Rigidbody2D playerRb;
    private Transform playertrans;

    [Header("原地探头查看（相机上下查看）")]
    private float peekHoldTimer;   //探头长按计时器
    public float peekRequiredHoldTime = 1f; // 需要长按1秒才激活探头

    [Header("移动使用组件")]
    public float speed;
    public float number;

    [Header("跳跃使用组件")]
    public float speedjump;
    private bool injump;
    private bool isFalling;
    private bool inground; // 地面检测变量
    public Transform feet;
    public LayerMask ground;
    [Header("人物可站立平台倾斜角度调整")]
    [Tooltip("地面检测半径。数值越大，人物能站在越倾斜的平台上（平台倾斜约45°建议0.15左右）。在面板上调整此值来适配不同倾斜角")]
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Header("攻击使用组件")]
    public bool attack;
    private float attackGuardTimer = 0f;
    public bool attackLocked = false;

    [Header("冲刺使用组件")]
    public float dashSpeed = 20f;
    public float dashTime = 0.3f;
    public float dashCooldown = 1f;
    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;
    private int dashDir;

    [Header("穿越平台使用组件")]
    public float Stime;

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
    [SerializeField] private LayerMask enemyLayers;

    [Header("完美弹反窗口")]
    public float perfectWindow = 0.2f;

    [Header("黑入模式")]
    [SerializeField] private float hackSlowTime = 0.1f;
    [SerializeField] private GameObject hackOverlay;
    [SerializeField] private UnityEngine.UI.Text hackTimerText;
    [SerializeField] private UnityEngine.UI.Text hackCooldownText;
    [SerializeField] private float hackMaxDuration = 8f;
    private List<FallingBullet> hackedBullets = new List<FallingBullet>();
    public bool hackingMode = false;
    private float hackTimer;
    private float hackCooldownTimer;
    private List<bossenemy> hackedTargets = new List<bossenemy>();

    [Header("普通格挡")]
    public float blockDamageReduction = 0.5f;

    [Header("算力系统")]
    [SerializeField] private int maxCyberPower = 100;
    [SerializeField] private int portalActivationCost = 15;
    [SerializeField] private float teleportCooldown = 10f;
    [SerializeField] private UnityEngine.UI.Text cyberPowerText;
    [SerializeField] private int coverActivationCost = 3;
    [SerializeField] private int boss2MissileHackCost = 5;
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
    public bool isBlocking;
    public bool perfectActive;

    [Header("最后一击")]
    [SerializeField] private float finalBlowKnockbackForce = 30f;
    [SerializeField] private float finalBlowKnockbackDuration = 1f;

    [HideInInspector] public bool controlsDisabled = false;

    [Header("死亡显示")]
    [SerializeField] private GameObject deathObject1;
    [SerializeField] private GameObject deathObject2;

    [Header("动画 Bool 值")]
    public bool attacktrue;
    public bool dfstrue;
    public bool defensedowntrue;
    public bool jumptrue;
    public bool dashtrue;
    public float runfloat;

    [Header("移动音效")]
    [SerializeField] private AudioClip moveSound;
    private AudioSource moveAudioSource;
    private bool moveSoundPlaying = false;

    [Header("房间传送过渡")]
    private bool isInRoomTransition = false;
    private float transitionHorizontalSpeed = 0f;

    public void SetHackCount(int count) => hackCount = count;
    public int GetHackCount() => hackCount;
    public void SetCyberPower(int power) { currentCyberPower = power; UpdateCyberUI(); }
    public int GetCyberPower() => currentCyberPower;
    public int GetCurrentCyberPower() => currentCyberPower;
    public void SetCyberEnabled(bool on) => cyberSystemEnabled = on;
    public bool IsCyberEnabled() => cyberSystemEnabled;

    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playertrans = GetComponent<Transform>();
        playerLayer = LayerMask.NameToLayer("player");
        platformLayerIndex = LayerMask.NameToLayer("platform");
        anim = GetComponent<Animator>();
        currentCyberPower = maxCyberPower;
        UpdateCyberUI();
        moveAudioSource = gameObject.AddComponent<AudioSource>();
        moveAudioSource.clip = moveSound;
        moveAudioSource.loop = true;
        moveAudioSource.playOnAwake = false;
        moveAudioSource.volume = 0.5f;
    }

    void Update()
    {
        // 普通控制禁用状态下直接返回（传送过渡除外）
        if (controlsDisabled && !isInRoomTransition) return;
        // 传送过渡状态下，只执行检测和动画，不响应输入
        if (!isInRoomTransition)
        {
            if (teleportCooldownTimer > 0)
                teleportCooldownTimer -= Time.unscaledDeltaTime;
            Hacker();
            Attacking();
            dash();
            JUMP();
            IgnoreLayer();
            Defense();
        }
        // 无论是否禁用控制，都执行地面检测和动画切换
        FixedupdateCheck();
        SwitchAnim();
        // 传送过渡时，单独处理朝向和动画参数
        if (isInRoomTransition)
        {
            if (transitionHorizontalSpeed > 0.1f)
                transform.localRotation = Quaternion.Euler(0, 0, 0);
            else if (transitionHorizontalSpeed < -0.1f)
                transform.localRotation = Quaternion.Euler(0, 180, 0);
            runfloat = Mathf.Abs(transitionHorizontalSpeed) / speed;
        }

        // ========== 探头逻辑：W/S长按1秒后触发上下探头 ==========
        bool peekBlocked = controlsDisabled || isDashing || isKnockedBack || isInRoomTransition;
        bool wHold = Input.GetKey(KeyCode.W);
        bool sHold = Input.GetKey(KeyCode.S);

        Vector2 peekDir = Vector2.zero;

        if (!peekBlocked && (wHold || sHold))
        {
            peekHoldTimer += Time.deltaTime;

            //长按时间达到1秒
            if (peekHoldTimer >= peekRequiredHoldTime)
            {
                if (wHold) peekDir = new Vector2(0f, 1f);
                else if (sHold) peekDir = new Vector2(0f, -1f);
            }
        }
        else
        {
            // 松开按键 / 被阻断：计时器清零，清除探头
            peekHoldTimer = 0f;
        }

        if (peekDir != Vector2.zero && !peekBlocked)
        {
            //Debug.Log($"探头激活，方向：{peekDir}，已按住时间:{peekHoldTimer:F2}");
            CameraZoneManager.Instance.SetPeekDirection(peekDir);
        }
        else
        {
            CameraZoneManager.Instance.ClearPeek();
        }
    }

    private void FixedUpdate()
    {
        cooldownTimer -= Time.deltaTime;

        // 传送过渡状态：强制水平移动
        if (isInRoomTransition)
        {
            playerRb.velocity = new Vector2(transitionHorizontalSpeed, playerRb.velocity.y);
            return;
        }

        if (controlsDisabled) return;
        move();
    }

    public void move()
    {
        if (isKnockedBack || attackLocked || isDashing) return;
        number = Input.GetAxis("Horizontal");
        playerRb.velocity = new Vector2(number * speed, playerRb.velocity.y);

        bool shouldPlay = inground && Mathf.Abs(number) > 0.1f;
        if (shouldPlay && !moveSoundPlaying)
        {
            moveAudioSource.Play();
            moveSoundPlaying = true;
        }
        else if (!shouldPlay && moveSoundPlaying)
        {
            moveAudioSource.Stop();
            moveSoundPlaying = false;
        }

        if (inground && Mathf.Abs(number) > 0.1f)
            runfloat = Mathf.Abs(number);
        else
            runfloat = 0f;

        if (!attack) File();
    }

    private void File()
    {
        if (playerRb.velocity.x > .1f)
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        if (playerRb.velocity.x < -.1f)
            transform.localRotation = Quaternion.Euler(0, 180, 0);
    }

    private void JUMP()
    {
        isFalling = playerRb.velocity.y < 0;
        if (Input.GetButtonDown("Jump") && inground)
        {
            playerRb.gravityScale = 6;
            playerRb.velocity = new Vector2(playerRb.velocity.x, speedjump);
            jumptrue = true;
        }

        if (playerRb.velocity.y > 0.1f && !inground)
            jumptrue = true;
        else if (playerRb.velocity.y <= 0 && !inground)
            jumptrue = false;

        if (inground)
            jumptrue = false;
    }

    private void FixedupdateCheck()
    {
        inground = Physics2D.OverlapCircle(feet.position, groundCheckRadius, ground);
    }

    public void dash()
    {
        float input = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && cooldownTimer <= 0 && Mathf.Abs(input) > 0.1f)
        {
            isDashing = true;
            dashtrue = true;
            dashTimer = dashTime;
            cooldownTimer = dashCooldown;
            dashDir = input > 0 ? 1 : -1;
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

    public void IgnoreLayer()
    {
        if (Input.GetKeyDown(KeyCode.S) || Input.GetButtonDown("Jump"))
        {
            Physics2D.IgnoreLayerCollision(playerLayer, platformLayerIndex, true);
            StartCoroutine(RestoreAfterTimer());
        }
    }

    public void Attacking()
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
            playerRb.velocity = new Vector2(0, playerRb.velocity.y);
            StartCoroutine(AttackUnlock());
        }
    }

    IEnumerator AttackUnlock()
    {
        yield return new WaitForSeconds(0.2f);
        attackLocked = false;
    }

    public void AttackEnd()
    {
        attack = false;
        attacktrue = false;
        controlsDisabled = false;
        attackGuardTimer = 0.1f;
    }

    public void Defense()
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
            isBlocking = false;
            perfectActive = false;
            defensedowntrue = true;
            StartCoroutine(DefenseDownTimer());
        }
    }

    IEnumerator DefenseDownTimer()
    {
        yield return new WaitForSeconds(0.3f);
        dfstrue = false;
        defensedowntrue = false;
    }

    public void Hacker()
    {
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

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!hackingMode && hackCooldownTimer <= 0)
                EnterHackMode();
        }

        if (hackingMode && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitHackMode(1f);
        }

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
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (hackTimerText != null) hackTimerText.text = Mathf.CeilToInt(hackTimer).ToString();
        if (hackCooldownText != null) hackCooldownText.text = "";
        if (hackOverlay != null) hackOverlay.SetActive(true);

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

        Portal[] portals = FindObjectsOfType<Portal>();
        foreach (Portal p in portals) p.SetForHack(true);

        Cover[] covers = FindObjectsOfType<Cover>();
        foreach (Cover c in covers) c.SetForHack(true);

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
        foreach (TrapControl t in traps) t.SetForHack(true);

        ZoneController[] zones = FindObjectsOfType<ZoneController>();
        foreach (ZoneController z in zones) z.SetForHack(true);

        if (sr != null) sr.color = new Color(0f, 1f, 1f);
    }

    public void HackEnemy(bossenemy target)
    {
        if (!hackingMode || target == null || target.isHacked) return;
        boss1ai boss = FindObjectOfType<boss1ai>();
        if (boss != null) target.GetHacked(boss.transform);
        IncrementHackCount();
        ExitHackMode(5f);
    }

    public void HackBullet(FallingBullet target)
    {
        if (!hackingMode || target == null || target.isHacked) return;
        boss1ai bossAI = FindObjectOfType<boss1ai>();
        if (bossAI != null) target.GetHacked(bossAI.transform);
        IncrementHackCount();
        ExitHackMode(5f);
    }

    void ExitHackMode(float cooldown)
    {
        hackingMode = false;
        hackCooldownTimer = cooldown;
        attackGuardTimer = 0.1f;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

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
        hackedBullets.Clear();

        Portal[] portals = FindObjectsOfType<Portal>();
        foreach (Portal p in portals) p.SetForHack(false);

        Cover[] covers = FindObjectsOfType<Cover>();
        foreach (Cover c in covers) c.SetForHack(false);

        foreach (Boss2Missile m in hackedBoss2Missiles)
        {
            if (m != null) m.SetHighlight(false);
        }
        hackedBoss2Missiles.Clear();

        TrapControl[] traps = FindObjectsOfType<TrapControl>();
        foreach (TrapControl t in traps) t.SetForHack(false);

        ZoneController[] zones = FindObjectsOfType<ZoneController>();
        foreach (ZoneController z in zones) z.SetForHack(false);

        if (sr != null) sr.color = Color.white;
    }

    public void ForceExitHackMode(float cooldown)
    {
        if (!hackingMode) return;
        ExitHackMode(cooldown);
    }

    private void SwitchAnim()
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
        if (isInvincible) return;
        health -= damage;
        StartCoroutine(InvincibilityRoutine());
        if (health <= 0)
        {
            controlsDisabled = true;
            if (deathObject1 != null) deathObject1.SetActive(true);
            if (deathObject2 != null) deathObject2.SetActive(true);

            attacktrue = false;
            dfstrue = false;
            defensedowntrue = false;
            jumptrue = false;
            dashtrue = false;
            runfloat = 0f;

            anim.SetBool("attacktrue", false);
            anim.SetBool("dfstrue", false);
            anim.SetBool("defensedowntrue", false);
            anim.SetBool("jumptrue", false);
            anim.SetBool("dashtrue", false);
            anim.SetFloat("runfloat", 0f);
            anim.SetTrigger("dietrue");
        }
    }

    public void OnDeathUICallback()
    {
        GameManager.Instance?.OnPlayerDied();
    }

    public void ActivateInvincibility()
    {
        if (!isInvincible)
            StartCoroutine(InvincibilityRoutine());
    }

    public void EnableCyberSystem()
    {
        cyberSystemEnabled = true;
        currentCyberPower = maxCyberPower - hackCount * 2;
        if (currentCyberPower < 0) currentCyberPower = 0;
        UpdateCyberUI();
    }

    public void IncrementHackCount()
    {
        hackCount++;
    }

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
        ExitHackMode(5f);
    }

    public void TryActivateTrap(TrapControl trap)
    {
        if (!hackingMode || trap == null) return;
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
        ExitHackMode(5f);
    }

    public void TryActivateZone(ZoneController zone)
    {
        if (!hackingMode || zone == null) return;
        IncrementHackCount();
        zone.Activate();
        ExitHackMode(5f);
    }

    public bool CanTeleport()
    {
        return teleportCooldownTimer <= 0;
    }

    public void OnTeleported()
    {
        teleportCooldownTimer = teleportCooldown;
    }

    void UpdateCyberUI()
    {
        if (cyberPowerText != null)
            cyberPowerText.text = "算力: " + currentCyberPower + "/" + maxCyberPower;
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

    public void DeliverFinalBlow(Transform boss, int ending)
    {
        isKnockedBack = true;
        float facing = transform.position.x > boss.position.x ? 1f : -1f;
        Vector2 dir = new Vector2(facing, 0.8f);
        playerRb.velocity = dir * finalBlowKnockbackForce;
        StartCoroutine(FinalBlowRoutine(boss, ending));
    }

    IEnumerator FinalBlowRoutine(Transform boss, int ending)
    {
        yield return new WaitForSeconds(finalBlowKnockbackDuration);
        isKnockedBack = false;
        boss2ai b2 = boss.GetComponent<boss2ai>();
        if (b2 != null) b2.FinalDeath();
        SceneGate gate = FindObjectOfType<SceneGate>();
        if (gate != null) gate.TriggerEnding(ending);
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        for (int i = 0; i < 32; i++)
        {
            if ((enemyLayers.value & (1 << i)) != 0)
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
            if ((enemyLayers.value & (1 << i)) != 0)
                Physics2D.IgnoreLayerCollision(playerLayer, i, false);
        }
        isInvincible = false;
    }

    IEnumerator RestoreAfterTimer()
    {
        yield return new WaitForSeconds(Stime);
        Physics2D.IgnoreLayerCollision(playerLayer, platformLayerIndex, false);
    }

    IEnumerator PerfectWindowTimer()
    {
        yield return new WaitForSeconds(perfectWindow);
        perfectActive = false;
    }

    public void StartRoomTransition(float moveDirection)
    {
        if (isDashing)
        {
            isDashing = false;
            dashtrue = false;
            playerRb.gravityScale = 3f;
        }

        isInRoomTransition = true;
        controlsDisabled = true;
        transitionHorizontalSpeed = moveDirection * speed;

        for (int i = 0; i < 32; i++)
        {
            if ((ground.value & (1 << i)) != 0 || i == platformLayerIndex)
                continue;
            Physics2D.IgnoreLayerCollision(playerLayer, i, true);
        }
    }

    public void EndRoomTransition()
    {
        isInRoomTransition = false;
        controlsDisabled = false;

        for (int i = 0; i < 32; i++)
        {
            if ((ground.value & (1 << i)) != 0 || i == platformLayerIndex)
                continue;
            Physics2D.IgnoreLayerCollision(playerLayer, i, false);
        }

        if (playerRb != null)
            playerRb.velocity = Vector2.zero;
    }
}