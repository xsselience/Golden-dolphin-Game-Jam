using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boss2ai : MonoBehaviour
{
    // ==================== 引用 ====================
    [Header("引用")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer bodySprite;

    [Header("指示灯")]
    [SerializeField] private SpriteRenderer leftLamp;
    [SerializeField] private SpriteRenderer rightLamp;

    // ==================== 血量与韧性 ====================
    [Header("血量与韧性")]
    [SerializeField] private int maxHealth = 500;
    [SerializeField] private int maxPoise = 100;
    [SerializeField] private float poiseDamageReduction = 0.1f;
    [SerializeField] private float poiseBreakDuration = 6f;

    private int currentHealth;
    private int currentPoise;
    private bool poiseBroken = false;
    private float poiseBreakTimer;
    private bool isDead = false;

    // ==================== 阶段 ====================
    public enum Phase { Phase1, Phase2, Phase3 }
    private Phase currentPhase = Phase.Phase1;
    [SerializeField] private float phase2Threshold = 0.55f;
    [SerializeField] private float phase3Threshold = 0.15f;
    private bool phase2Triggered = false;
    private bool phase3Triggered = false;

    // ==================== 技能系统 ====================
    [Header("技能冷却 - 阶段1/2")]
    [SerializeField] private float phase1GlobalCooldown = 3f;
    [SerializeField] private float phase2GlobalCooldown = 1.5f;

    [Header("技能冷却 - 阶段3（独立）")]
    [SerializeField] private float skill1Cooldown = 4f;
    [SerializeField] private float skill2Cooldown = 5f;
    [SerializeField] private float skill3Cooldown = 6f;
    [SerializeField] private float skill4Cooldown = 3f;

    private int currentSkillIndex = 0;
    private float[] lastSkillTimes = new float[4];
    private float globalCooldownTimer;

    // ==================== 技能动画 Bool ====================
    [Header("动画信号")]
    [SerializeField] private bool skillMissile;    // 技能1
    [SerializeField] private bool skillLaser;      // 技能2
    [SerializeField] private bool skillSlow;       // 技能3
    [SerializeField] private bool skillBombard;    // 技能4

    // 激光子方向（动画事件里判读）
    private bool laserTop;

    // ==================== 技能预制体 ====================
    [Header("技能1：导弹")]
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private GameObject redLinePrefab;
    [SerializeField] private float missileHeight = 12f;
    [SerializeField] private int missileDamage = 25;

    // ==================== 技能2：激光 ====================
    [Header("技能2：激光")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private GameObject laserSpawnPoint;   // Boss 子级的空物体
    [SerializeField] private float laserSweepDuration = 2f;

    [Header("技能3：减速区域")]
    [SerializeField] private GameObject slowZonePrefab;
    [SerializeField] private float slowZoneDuration = 4f;
    [SerializeField] private float slowAmount = 0.3f;

    [Header("技能4：随机轰炸")]
    [SerializeField] private int missileCount = 8;
    [SerializeField] private float bombRadius = 10f;

    [Header("激活区域")]
    [SerializeField] private Vector2 activationZoneSize = new Vector2(20f, 10f);  // 矩形大小
    [SerializeField] private Vector2 activationZoneOffset = Vector2.zero;          // 中心偏移
    [SerializeField] private LayerMask playerLayer;
    private bool isActive = false;
    // ==================== 内部状态 ====================
    private enum BossState { Idle, Casting }
    private BossState currentState = BossState.Idle;
    private bool isActing = false;

    // ==================== 初始化 ====================
    void Start()
    {
        currentHealth = maxHealth;
        currentPoise = 0;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (animator == null)
            animator = GetComponent<Animator>();

        for (int i = 0; i < 4; i++)
            lastSkillTimes[i] = -999f;

        if (leftLamp != null) leftLamp.color = Color.gray;
        if (rightLamp != null) rightLamp.color = Color.gray;
    }

    // ==================== 主循环 ====================
    void Update()
    {
        if (!isActive)
        {
            CheckActivation();
            return;   // 没激活，什么都不做
        }

        if (isDead) return;

        SwitchSkillAnims();

        if (poiseBroken)
        {
            poiseBreakTimer -= Time.deltaTime;
            if (poiseBreakTimer <= 0)
            {
                poiseBroken = false;
                currentPoise = 0;
                if (bodySprite != null) bodySprite.color = Color.white;
            }
        }

        CheckPhase();

        if (!isActing && currentState == BossState.Idle)
        {
            RunSkillCycle();
        }
    }

    // ==================== 阶段检测 ====================
    void CheckPhase()
    {
        float hpPercent = (float)currentHealth / maxHealth;

        if (hpPercent <= phase3Threshold && !phase3Triggered)
        {
            phase3Triggered = true;
            currentPhase = Phase.Phase3;
            if (rightLamp != null) rightLamp.color = Color.red;
            Debug.Log("Boss2 进入阶段3！");
        }
        else if (hpPercent <= phase2Threshold && !phase2Triggered)
        {
            phase2Triggered = true;
            currentPhase = Phase.Phase2;
            if (leftLamp != null) leftLamp.color = Color.red;
            Debug.Log("Boss2 进入阶段2！");
        }
    }

    // ==================== 技能循环 ====================
    void RunSkillCycle()
    {
        switch (currentPhase)
        {
            case Phase.Phase1:
            case Phase.Phase2:
                RunSequentialSkills();
                break;
            case Phase.Phase3:
                RunIndependentSkills();
                break;
        }
    }

    void RunSequentialSkills()
    {
        float cooldown = currentPhase == Phase.Phase2 ? phase2GlobalCooldown : phase1GlobalCooldown;
        if (Time.time - globalCooldownTimer < cooldown) return;

        TriggerSkill(currentSkillIndex);
        currentSkillIndex = (currentSkillIndex + 1) % 4;
    }

    void RunIndependentSkills()
    {
        for (int i = 0; i < 4; i++)
        {
            float cd = GetSkillCooldown(i);
            if (Time.time - lastSkillTimes[i] >= cd)
            {
                TriggerSkill(i);
                break;
            }
        }
    }

    float GetSkillCooldown(int index)
    {
        return index switch
        {
            0 => skill1Cooldown,
            1 => skill2Cooldown,
            2 => skill3Cooldown,
            3 => skill4Cooldown,
            _ => 4f
        };
    }

    /// <summary>
    /// 触发技能动画 bool，生成逻辑全由动画事件接管。
    /// </summary>
    void TriggerSkill(int index)
    {
        isActing = true;
        currentState = BossState.Casting;

        switch (index)
        {
            case 0:
                skillMissile = true;
                break;
            case 1:
                laserTop = Random.value > 0.5f;
                skillLaser = true;
                break;
            case 2:
                skillSlow = true;
                break;
            case 3:
                skillBombard = true;
                break;
        }
    }

    void SwitchSkillAnims()
    {
        animator.SetBool("skillMissile", skillMissile);
        animator.SetBool("skillLaser", skillLaser);
        animator.SetBool("skillSlow", skillSlow);
        animator.SetBool("skillBombard", skillBombard);
    }

    // ==================== 技能1：导弹（动画事件）====================

    /// <summary>动画事件：施法完成，生成红线和延时导弹。</summary>
    public void OnMissileSpawn()
    {
        float playerX = player.position.x;
        Vector3 spawnPos = new Vector3(playerX, player.position.y + missileHeight, 0);

        if (redLinePrefab != null)
        {
            GameObject line = Instantiate(redLinePrefab, player.position, Quaternion.identity);
            RedLine rl = line.GetComponent<RedLine>();
            if (rl != null) rl.SetTarget(spawnPos.y);
        }

        StartCoroutine(DelayedMissile(spawnPos, 1.5f));
    }

    /// <summary>动画事件：导弹动画结束，进入冷却。</summary>
    public void OnMissileEnd()
    {
        skillMissile = false;
        EndSkill(0);
    }

    // ==================== 技能2：激光（动画事件）====================

    /// <summary>动画事件：激光开始，生成激光物体。</summary>
    public void OnLaserStart()
    {
        float halfH = activationZoneSize.y / 2f;
        float centerY = transform.position.y + activationZoneOffset.y;

        float topY, bottomY;
        if (laserTop)
        {
            // 上半区：最顶部 → 中间
            topY = centerY + halfH;
            bottomY = centerY;
        }
        else
        {
            // 下半区：中间 → 最底部
            topY = centerY;
            bottomY = centerY - halfH;
        }

        float rightEdge = transform.position.x + activationZoneOffset.x + activationZoneSize.x / 2f;
        Vector3 startPoint = new Vector3(rightEdge, 0, 0);

        GameObject laser = Instantiate(laserPrefab, startPoint, Quaternion.identity);

        LaserBeam lb = laser.GetComponent<LaserBeam>();
        if (lb != null)
            lb.Init(laserTop, topY, bottomY, startPoint);
    }

    /// <summary>动画事件：动画结束，进入冷却。</summary>
    public void OnLaserEnd()
    {
        skillLaser = false;
        EndSkill(1);
    }

    // ==================== 技能3：减速区域（动画事件）====================

    /// <summary>动画事件：生成减速区域。</summary>
    public void OnSlowZoneSpawn()
    {
        GameObject zone = Instantiate(slowZonePrefab, player.position, Quaternion.identity);
        SlowZone sz = zone.GetComponent<SlowZone>();
        if (sz != null)
        {
            sz.Init(slowAmount, slowZoneDuration);
        }
    }

    /// <summary>动画事件：结束，进入冷却。</summary>
    public void OnSlowZoneEnd()
    {
        skillSlow = false;
        EndSkill(2);
    }

    // ==================== 技能4：随机轰炸（动画事件）====================

    /// <summary>动画事件：施法完成，生成全部红线和延时导弹。</summary>
    public void OnBombardSpawn()
    {
        for (int i = 0; i < missileCount; i++)
        {
            float randX = player.position.x + Random.Range(-bombRadius, bombRadius);
            float randY = player.position.y + Random.Range(0f, 4f);
            Vector3 bombPos = new Vector3(randX, randY, 0);
            Vector3 spawnPos = new Vector3(randX, randY + missileHeight, 0);

            if (redLinePrefab != null)
            {
                GameObject line = Instantiate(redLinePrefab, bombPos, Quaternion.identity);
                RedLine rl = line.GetComponent<RedLine>();
                if (rl != null) rl.SetTarget(spawnPos.y);
            }

            StartCoroutine(DelayedMissile(spawnPos, 1f + Random.Range(0f, 1f)));
        }
    }

    /// <summary>动画事件：轰炸结束，进入冷却。</summary>
    public void OnBombardEnd()
    {
        skillBombard = false;
        EndSkill(3);
    }

    // ==================== 辅助 ====================

    IEnumerator DelayedMissile(Vector3 pos, float delay)
    {
        yield return new WaitForSeconds(delay);
        GameObject missile = Instantiate(missilePrefab, pos, Quaternion.identity);
        FallingBullet fb = missile.GetComponent<FallingBullet>();
        if (fb != null) fb.damage = missileDamage;
    }

    void EndSkill(int skillIndex)
    {
        isActing = false;
        currentState = BossState.Idle;

        if (currentPhase == Phase.Phase1 || currentPhase == Phase.Phase2)
            globalCooldownTimer = Time.time;
        else
            lastSkillTimes[skillIndex] = Time.time;
    }

    // ==================== 受伤 ====================

    public void TakeDamage(int rawDamage)
    {
        if (isDead) return;

        if (!poiseBroken)
        {
            currentPoise += rawDamage;
            if (currentPoise >= maxPoise)
                BreakPoise();
        }

        int actualDamage = poiseBroken ? rawDamage : Mathf.RoundToInt(rawDamage * poiseDamageReduction);
        if (actualDamage < 1) actualDamage = 1;
        currentHealth -= actualDamage;

        if (currentHealth <= 0) Die();
    }

    void CheckActivation()//激活boss
    {
        Vector2 center = (Vector2)transform.position + activationZoneOffset;
        Collider2D hit = Physics2D.OverlapBox(center, activationZoneSize, 0f, playerLayer);

        if (hit != null)
        {
            isActive = true;
            Debug.Log("Boss2 已激活！");

        }
        if (isActive)
        {
            player p = hit.GetComponent<player>();
            if (p != null)
                p.EnableCyberSystem();
        }
    }

    void OnDrawGizmosSelected()//绘制激活区域
    {
        Vector2 center = (Vector2)transform.position + activationZoneOffset;

        if (!isActive)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(center, activationZoneSize);
    }

    /// <summary>
    /// 棱镜反弹调用：立刻充满韧性条并击破。
    /// </summary>
    public void InstantlyBreakPoise()
    {
        if (poiseBroken || isDead) return;

        currentPoise = maxPoise;
        BreakPoise();
        Debug.Log("棱镜反弹！Boss2 韧性击破");
    }

    void BreakPoise()
    {
        Debug.Log("BreakPoise 被调用，bodySprite=" + (bodySprite != null));   // ← 加这行
        poiseBroken = true;
        poiseBreakTimer = poiseBreakDuration;
        if (bodySprite != null) bodySprite.color = Color.yellow;
        Debug.Log("Boss2 韧性击破！减伤消失");
    }

    void Die()
    {
        isDead = true;
        StopAllCoroutines();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
        enabled = false;
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public int GetCurrentPoise() => currentPoise;
    public int GetMaxPoise() => maxPoise;
    public bool IsPoiseBroken() => poiseBroken;
    public Phase GetPhase() => currentPhase;
}
