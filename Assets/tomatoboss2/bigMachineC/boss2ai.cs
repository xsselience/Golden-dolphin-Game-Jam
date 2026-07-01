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

    [Header("场景机关")]
    [SerializeField] private Transform activateDropObject;
    [SerializeField] private float activateDropDistance = 5f;
    [SerializeField] private float activateDropSpeed = 3f;

    [Header("当前状态")]
    public int currentHealth;
    public int currentPoise;
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

    [Header("受击点")]
    [SerializeField] private int deactivateInPhase2 = 1;   // 进阶段 2 失效第几个（索引）
    [SerializeField] private int deactivateInPhase3 = 0;   // 进阶段 3 再失效第几个
    private Boss2HitPoint[] hitPoints = new Boss2HitPoint[3];

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
    //[SerializeField] private float laserSweepDuration = 2f;
    [Header("技能2：激光预警物体")]
    [SerializeField] private GameObject leftWarningObj;    // 往左扫前闪烁
    [SerializeField] private GameObject rightWarningObj;   // 往右扫前闪烁
    private bool warningDone = false;

    private bool laserRightHalf;

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

    [Header("死亡演出")]
    [SerializeField] private float deathZoomDuration = 2f;      // 镜头拉近多久
    [SerializeField] private float deathZoomSize = 2f;          // 放大后 orthographicSize
    [SerializeField] private float deathHoldTime = 1.5f;        // 停在 Boss 多久
    [SerializeField] private Camera mainCam;

    private bool isDying = false;
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
        if (leftWarningObj != null) leftWarningObj.SetActive(false);
        if (rightWarningObj != null) rightWarningObj.SetActive(false);
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

            // 失效一个受击点
            if (deactivateInPhase3 >= 0 && deactivateInPhase3 < 3 && hitPoints[deactivateInPhase3] != null)
                hitPoints[deactivateInPhase3].SetActive(false);

            Debug.Log("Boss2 进入阶段3！");
        }
        else if (hpPercent <= phase2Threshold && !phase2Triggered)
        {
            phase2Triggered = true;
            currentPhase = Phase.Phase2;
            if (leftLamp != null) leftLamp.color = Color.red;

            // 失效一个受击点
            if (deactivateInPhase2 >= 0 && deactivateInPhase2 < 3 && hitPoints[deactivateInPhase2] != null)
                hitPoints[deactivateInPhase2].SetActive(false);

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
                laserRightHalf = Random.value > 0.5f;
                skillLaser = true;

                // 上方生成时左右取反
                bool warnRight = laserTop ? !laserRightHalf : laserRightHalf;
                StartCoroutine(BlinkWarning(warnRight ? rightWarningObj : leftWarningObj));
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
    IEnumerator BlinkWarning(GameObject obj)
    {
        if (obj == null) yield break;

        warningDone = false;
        obj.SetActive(true);

        for (int i = 0; i < 2; i++)
        {
            obj.SetActive(false);
            yield return new WaitForSeconds(0.15f);
            obj.SetActive(true);
            yield return new WaitForSeconds(0.15f);
        }

        warningDone = true;
    }

    /// <summary>动画事件：激光开始，生成激光物体。</summary>
    public void OnLaserStart()
    {
        // 等闪烁完成再隐藏
        if (!warningDone)
        {
            StartCoroutine(HideAfterWarning());
            return;
        }

        HideWarningAndFire();
        if (leftWarningObj != null) leftWarningObj.SetActive(false);
        if (rightWarningObj != null) rightWarningObj.SetActive(false);

        float range = Mathf.Max(activationZoneSize.x, activationZoneSize.y);
        Vector3 startPoint = laserSpawnPoint != null ? laserSpawnPoint.transform.position : transform.position;

        GameObject laser = Instantiate(laserPrefab, startPoint, Quaternion.identity);

        LaserBeam lb = laser.GetComponent<LaserBeam>();
        if (lb != null)
            lb.Init(startPoint, laserTop, laserRightHalf, range);
    }

    /// <summary>动画事件：动画结束，进入冷却。</summary>
    public void OnLaserEnd()
    {
        skillLaser = false;
        EndSkill(1);
    }


    IEnumerator HideAfterWarning()
    {
        while (!warningDone) yield return null;
        HideWarningAndFire();
    }

    void HideWarningAndFire()
    {
        if (leftWarningObj != null) leftWarningObj.SetActive(false);
        if (rightWarningObj != null) rightWarningObj.SetActive(false);

        float range = Mathf.Max(activationZoneSize.x, activationZoneSize.y);
        Vector3 startPoint = laserSpawnPoint != null ? laserSpawnPoint.transform.position : transform.position;
        GameObject laser = Instantiate(laserPrefab, startPoint, Quaternion.identity);

        LaserBeam lb = laser.GetComponent<LaserBeam>();
        if (lb != null) lb.Init(startPoint, laserTop, laserRightHalf, range);
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
        if (isDead || isDying) return;

        // 锁血 1
        int potentialHealth = currentHealth;
        if (!poiseBroken)
        {
            currentPoise += rawDamage;
            if (currentPoise >= maxPoise) BreakPoise();
        }

        int actualDamage = poiseBroken ? rawDamage : Mathf.RoundToInt(rawDamage * poiseDamageReduction);
        if (actualDamage < 1) actualDamage = 1;
        potentialHealth -= actualDamage;

        if (potentialHealth <= 0)
        {
            currentHealth = 1;           // 锁 1 滴血
            StartCoroutine(DeathSequence());
            return;
        }

        currentHealth = potentialHealth;
    }
    public void RegisterHitPoint(Boss2HitPoint hp)
    {
        int idx = hp.GetIndex();
        if (idx >= 0 && idx < 3)
            hitPoints[idx] = hp;
    }

    void CheckActivation()//激活boss
    {
        Vector2 center = (Vector2)transform.position + activationZoneOffset;
        Collider2D hit = Physics2D.OverlapBox(center, activationZoneSize, 0f, playerLayer);

        if (hit != null)
        {
            isActive = true;

            if (activateDropObject != null)
                StartCoroutine(MoveDown(activateDropObject, activateDropDistance, activateDropSpeed));
            player p = hit.GetComponent<player>();
            if (p != null)
            {
                p.EnableCyberSystem();
            }
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

    IEnumerator DeathSequence()
    {
        Debug.Log("=== DeathSequence 开始 ===");

        // 禁用玩家操作
        player p = player.GetComponent<player>();
        if (p != null) p.controlsDisabled = true;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null)
        {
            if (p != null) p.controlsDisabled = false;
            yield break;
        }

        isActing = false;
        skillMissile = false;
        skillLaser = false;
        skillSlow = false;
        skillBombard = false;

        Vector3 camStartPos = mainCam.transform.position;
        float startSize = mainCam.orthographicSize;
        Vector3 bossPos = new Vector3(transform.position.x, transform.position.y, camStartPos.z);

        Debug.Log($"镜头开始拉近: {camStartPos} → {bossPos}, size {startSize} → {deathZoomSize}");

        float elapsed = 0f;
        while (elapsed < deathZoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathZoomDuration;
            mainCam.transform.position = Vector3.Lerp(camStartPos, bossPos, t);
            mainCam.orthographicSize = Mathf.Lerp(startSize, deathZoomSize, t);
            yield return null;
        }

        mainCam.transform.position = bossPos;
        mainCam.orthographicSize = deathZoomSize;
        Debug.Log("镜头已到达 Boss，等待 " + deathHoldTime + " 秒");

        yield return new WaitForSeconds(deathHoldTime);

        Debug.Log("镜头弹回玩家");
        if (player == null)
        {
            if (p != null) p.controlsDisabled = false;
            yield break;
        }

        Vector3 playerPos = new Vector3(player.position.x, player.position.y, camStartPos.z);
        float returnDuration = 0.4f;
        elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            mainCam.transform.position = Vector3.Lerp(bossPos, playerPos, t);
            mainCam.orthographicSize = Mathf.Lerp(deathZoomSize, startSize, t);
            yield return null;
        }

        mainCam.transform.position = playerPos;
        mainCam.orthographicSize = startSize;

        // 恢复玩家操作
        if (p != null) p.controlsDisabled = false;

        Debug.Log("调用 DeliverFinalBlow");
        if (p != null)
            p.DeliverFinalBlow(transform, CheckEnding());
        else
            Debug.LogError("player 脚本获取失败！");
    }

    /// <summary>算力不为0→结局2，为0→结局1</summary>
    int CheckEnding()
    {
        player p = player.GetComponent<player>();
        if (p != null && p.GetCurrentCyberPower() > 0) return 2;
        return 1;
    }

    public void FinalDeath()
    {
        isDead = true;
        currentHealth = 0;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
        enabled = false;

        Debug.Log("Boss2 最终死亡");
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public int GetCurrentPoise() => currentPoise;
    public int GetMaxPoise() => maxPoise;
    public bool IsPoiseBroken() => poiseBroken;
    public Phase GetPhase() => currentPhase;

    IEnumerator MoveDown(Transform obj, float distance, float speed)
    {
        Vector3 endPos = obj.position + Vector3.down * distance;

        while (Vector3.Distance(obj.position, endPos) > 0.02f)
        {
            obj.position = Vector3.MoveTowards(obj.position, endPos, speed * Time.deltaTime);
            yield return null;
        }
        obj.position = endPos;
    }
}
