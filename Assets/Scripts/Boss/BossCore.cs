using System.Collections;
using UnityEngine;

public class BossCore : MonoBehaviour
{
    public enum BossState
    {
        flying,
        glideAttack,
        slamAttack,
        FireBallAttack,
        Dead,
    }
    public int facingDirection = 1;
    public float floatSpeed = 2f;
    public float floatDistance = 0.5f;
    public float minFlyDuration = 1f;
    public float maxFlyDuration = 2f;
    public float normalFlySpeed = 4f;
    public Transform flyPositionLeftRange;
    public Transform flyPositionRightRange;
    [Range(0f, 100f)]
    public float GlideWeight = 40f;
    [Range(0f, 100f)]
    public float SlamWeight = 40f;
    [Range(0f, 100f)]
    public float FireWeight = 20f;
    public bossSound sound;
    public bool canAttack = false;

    [Header("Glide Attack")]
    public Transform glideRightPosition;
    public Transform glideLeftPosition;
    public float waitBeforeGlideDuration = 1f;
    public float glideSpeed = 15f;
    public float waitAfterGlideDuration = 1.5f;
    public float startYOffset = -0.8f;

    [Header("Slam Attack")]
    public float groundY = 0f;
    public float waitBeforeSlamDuration = 0.5f;
    public GameObject slamShockwave;
    public float shockWaveDuration = 0.3f;
    public float waitAfterSlamDuration = 1f;
    public float slamSpeed = 18f;
    public float flyAwaySpeed = 18f;
    public Transform bossBody;

    [Header("FireBall Attack")]
    public float waitBeforeFireBallDuration = 0.3f;
    public GameObject fireBall;
    public Transform[] fireballTargets = null;
    public Transform fireballSource;
    public float fireBallTimeToTarget = 1f;
    public float waitAfterFireBallDuration = 0.3f;
    public float fireBallInitalYSpeedMin = 10f;
    public float fireBallInitalYSpeedMax = 25f;
    public float fireBallDestenationOffsetMax = 0.5f;
    public int fireBallWaves = 3;
    public float waitBetweenFireBallWavesDuration = 0.5f;

    [Header("Getting Hit")]
    public SpriteRenderer spriteRenderer;
    public Material getHitMaterial;
    public float invincibleDuration = 0.2f;

    [Header("Death")]
    public ParticleSystem deathEffect;
    public float deathEffectDuration = 5f;
    
    [Header("Health Bar")]
    public GameObject healthBarPrefab;
    private HealthBarWorld healthBarWorld;
    public Transform healthBarAnchor;

    private Rigidbody2D rb;
    private Health health;
    private Animator anim;
    private Animator shockWaveAnim;
    private BossState state;
    private Transform target;
    private Material normalMaterial;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        anim = GetComponent<Animator>();
        shockWaveAnim = slamShockwave.GetComponent<Animator>();
        normalMaterial = spriteRenderer.material;
        GoToFlyingState();
    }
    private void Start()
    {
        Vector3 scale = transform.localScale;
        scale.x = facingDirection;
        transform.localScale = scale;
        startFloatPosition = transform.position;
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player").transform;
        }
        if (healthBarPrefab != null)
        {
            var go = Instantiate(healthBarPrefab, healthBarAnchor.position, Quaternion.identity);
            go.transform.SetParent(healthBarAnchor, worldPositionStays: true);
            healthBarWorld = go.GetComponentInChildren<HealthBarWorld>();
            healthBarWorld.Setup(health.maxHealth);
            healthBarWorld.SetHealth(health.currentHealth, health.maxHealth);
        }
    }
    private void OnEnable()
    {
        health.OnDamaged += Health_OnDamaged;
    }
    private void OnDisable()
    {
        health.OnDamaged -= Health_OnDamaged;
    }
    private void Health_OnDamaged(HitInfo info)
    {
        sound.GetHit();
        
        if (healthBarWorld != null)
            healthBarWorld.SetHealth(health.currentHealth, health.maxHealth);
        
        if (health.currentHealth == 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(GetHitSequence());
        }
    }

    private void Die()
    {
        sound.Die();
        StopAllCoroutines();
        state = BossState.Dead;
        health.SetCanHit(false);
        anim.Play("Slam");
        TurnOffAttackBoxes();
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        transform.rotation = Quaternion.identity;
        StartCoroutine(DeathSequence());
    }
    IEnumerator DeathSequence()
    {
        deathEffect.Play();
        float timer = deathEffectDuration;
        while (timer > 0)
        {
            timer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        deathEffect.Stop();
        rb.isKinematic = false;
        anim.Play("Dead");
        rb.gravityScale = 2;
        yield return new WaitForSecondsRealtime(1.5f);
        GameManager.gm.BeatBoss();
    }

    private void TurnOffAttackBoxes()
    {
        var attackBoxes = GetComponentsInChildren<AttackBox>();
        for (int i = 0; i < attackBoxes.Length; i++)
        {
            attackBoxes[i].GetComponent<Collider2D>().enabled = false;
        }
    }

    IEnumerator GetHitSequence()
    {
        spriteRenderer.material = getHitMaterial;
        health.SetCanHit(false);
        float timer = invincibleDuration;
        while (timer > 0)
        {
            timer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        health.SetCanHit(true);
        spriteRenderer.material = normalMaterial;

    }

    private void FixedUpdate()
    {
        //Debug.Log($"[Boss] Current State: {state}");
        switch (state)
        {
            case BossState.flying:
                FlyingState();
                break;
            case BossState.glideAttack:
                GlideAttackState();
                break;
            case BossState.slamAttack:
                SlamAttackState();
                break;
            case BossState.FireBallAttack:
                FireBallAttackState();
                break;
            default:
                break;
        }
    }

    private Vector3 startFloatPosition;
    private int floatDirection = -1;
    private float flyTimer = 0f;
    private void FlyingState()
    {
        Float();
        if (!canAttack) return;
        if (flyTimer > Mathf.Epsilon)
        {
            flyTimer -= Time.fixedDeltaTime;
        }
        else
        {
            if (GlideWeight + SlamWeight + FireWeight <= 0f) return; 
            float action = Random.Range(0f, GlideWeight + SlamWeight + FireWeight);
            if (action < GlideWeight)
            {
                state = BossState.glideAttack;
            }
            else if (action < GlideWeight + SlamWeight)
            {
                state = BossState.slamAttack;
            }
            else
            {
                state = BossState.FireBallAttack;
            }
        }
    }

    private void Float()
    {
        if (Vector3.Distance(transform.position, startFloatPosition) >= floatDistance)
        {
            startFloatPosition = transform.position;
            floatDirection *= -1;
        }
        rb.linearVelocity = floatSpeed * floatDirection * Vector2.up;

    }

    private Coroutine glideRoutine = null;
    private void GlideAttackState()
    {
        if (glideRoutine == null)
        {
            glideRoutine = StartCoroutine(GlideAttackSequence());
        }
    }
    IEnumerator GlideAttackSequence()
    {
        bool chooseRight = Random.Range(0f, 1f) < 0.5f;
        Vector3 startPosition;
        Vector3 endPosition;
        if (chooseRight)
        {
            startPosition = glideRightPosition.position;
            endPosition = glideLeftPosition.position;
        }
        else
        {
            startPosition = glideLeftPosition.position;
            endPosition = glideRightPosition.position;
        }
        while (Vector3.Distance(transform.position, startPosition) > 0.3f)
        {
            Vector3 moveDirection = startPosition - transform.position;
            Vector2 moveVelocity = moveDirection.normalized * normalFlySpeed;
            rb.linearVelocity = moveVelocity;
            FaceVelocity();
            yield return new WaitForFixedUpdate();
        }
        float waitTimer = waitBeforeGlideDuration;
        startFloatPosition = transform.position;
        rb.linearVelocity = Vector2.zero;
        FacePosition(endPosition);
        anim.Play("Fly");
        while (waitTimer > 0)
        {
            waitTimer -= Time.fixedDeltaTime;
            Float();
            yield return new WaitForFixedUpdate();
        }
        rb.linearVelocity = Vector2.zero;
        Quaternion savedRotation = transform.rotation;
        transform.position += new Vector3(0, startYOffset, 0);
        endPosition += new Vector3(0, startYOffset, 0);
        Utility.RotateTowards(endPosition, transform);
        anim.Play("Slam");
        sound.Glide();
        while (Vector3.Distance(transform.position, endPosition) > 0.3f)
        {
            Vector3 moveDirection = endPosition - transform.position;
            Vector2 moveVelocity = moveDirection.normalized * glideSpeed;
            rb.linearVelocity = moveVelocity;
            FaceVelocity();
            yield return new WaitForFixedUpdate();
        }
        transform.position -= new Vector3(0, startYOffset, 0);
        transform.rotation = savedRotation;
        waitTimer = waitBeforeGlideDuration;
        anim.Play("Fly");
        startFloatPosition = transform.position;
        rb.linearVelocity = Vector2.zero;
        while (waitTimer > 0)
        {
            waitTimer -= Time.fixedDeltaTime;
            Float();
            yield return new WaitForFixedUpdate();
        }
        Vector3 targetPosition = flyPositionLeftRange.position;
        targetPosition.x = Random.Range(flyPositionLeftRange.position.x, flyPositionRightRange.position.x);
        while (Vector3.Distance(transform.position, targetPosition) > 0.3f)
        {
            Vector3 moveDirection = targetPosition - transform.position;
            Vector2 moveVelocity = moveDirection.normalized * normalFlySpeed;
            rb.linearVelocity = moveVelocity;
            FaceVelocity();
            yield return new WaitForFixedUpdate();
        }
        rb.linearVelocity = Vector2.zero;
        FacePosition(target.position);
        GoToFlyingState();
        glideRoutine = null;
    }

    private Coroutine slamRoutine = null;
    private void SlamAttackState()
    {
        if (slamRoutine == null)
        {
            slamRoutine = StartCoroutine(SlamAttackSequence());
        }
    }

    IEnumerator SlamAttackSequence()
    {
        float waitTimer = waitBeforeSlamDuration;
        rb.linearVelocity = Vector2.zero;
        anim.Play("Slam");
        while (waitTimer > 0)
        {
            waitTimer -= Time.fixedDeltaTime;
            //FlyingState();
            yield return new WaitForFixedUpdate();
        }
        Vector3 slamPosition = target.position;
        slamPosition.y = groundY;
        slamPosition.x = Mathf.Clamp(slamPosition.x, flyPositionLeftRange.position.x, flyPositionRightRange.position.x);
        FacePosition(slamPosition);
        Quaternion savedRotation = transform.rotation;
        Utility.RotateTowards(slamPosition, transform);
        anim.Play("Slam");
        sound.Whoosh();
        float timeout = 0.5f;
        while (Vector3.Distance(transform.position, slamPosition) > 0.3f && timeout > 0f)
        {
            Vector3 moveDirection = slamPosition - transform.position;
            Vector2 moveVelocity = moveDirection.normalized * slamSpeed;
            rb.linearVelocity = moveVelocity;
            timeout -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (timeout <= 0f)
        {
            Debug.LogWarning("[Boss] Timeout while moving to slamPosition");
        }

        sound.Slam();
        anim.Play("Fly");
        transform.rotation = savedRotation;
        waitTimer = shockWaveDuration;
        rb.linearVelocity = Vector2.zero;
        slamShockwave.SetActive(true);
        shockWaveAnim.Play("Slam");
        while (waitTimer > 0)
        {
            waitTimer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        shockWaveAnim.Play("Nothing");
        slamShockwave.SetActive(false);
        waitTimer = waitAfterSlamDuration;
        startFloatPosition = transform.position;
        while (waitTimer > 0)
        {
            waitTimer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        Vector3 targetPosition = flyPositionLeftRange.position;
        targetPosition.x = transform.position.x;
        waitTimer = shockWaveDuration;
        slamShockwave.SetActive(true);
        shockWaveAnim.Play("FlyAway");
        anim.Play("Slam");
        Vector3 shockwavePosition = slamShockwave.transform.localPosition;
        slamShockwave.transform.parent = null;
        while (Vector3.Distance(transform.position, slamPosition) > 0.3f && timeout > 0f)
        {
            Vector3 moveDirection = slamPosition - transform.position;
            Vector2 moveVelocity = moveDirection.normalized * slamSpeed;
            rb.linearVelocity = moveVelocity;
            timeout -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (timeout <= 0f)
        {
            Debug.LogWarning("[Boss] Timeout while moving to slamPosition");
        }

        anim.Play("Fly");
        if (waitTimer > 0 && waitTimer <= shockWaveDuration + Mathf.Epsilon)
        {
            while (waitTimer > 0)
            {
                waitTimer -= Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            shockWaveAnim.Play("Nothing");
            slamShockwave.SetActive(false);
            slamShockwave.transform.parent = transform;
            slamShockwave.transform.localPosition = shockwavePosition;
        }

        rb.linearVelocity = Vector2.zero;
        FacePosition(target.position);
        GoToFlyingState();
        slamRoutine = null;
    }



    private void FireBallAttackState()
    {
        if (fireBallRoutine == null)
        {
            fireBallRoutine = StartCoroutine(FireBallAttackSequence());
        }
    }

    private Coroutine fireBallRoutine = null;
    IEnumerator FireBallAttackSequence()
    {
        float savedGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        bool chooseRight = Random.Range(0f, 1f) < 0.5f;
        chooseRight = true;
        Vector3 startPosition;
        if (chooseRight)
        {
            startPosition = flyPositionRightRange.position;
        }
        else
        {
            startPosition = flyPositionLeftRange.position;
        }
        while (Vector3.Distance(transform.position, startPosition) > 0.3f)
        {
            Vector3 moveDirection = startPosition - transform.position;
            Vector2 moveVelocity = moveDirection.normalized * normalFlySpeed;
            rb.linearVelocity = moveVelocity;
            FaceVelocity();
            yield return new WaitForFixedUpdate();
        }
        FacePosition(flyPositionLeftRange.position);
        float waitTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        for (int j = 0; j < fireBallWaves; j++)
        {
            GameObject[] fireBalls = new GameObject[fireballTargets.Length];
            for (int i = 0; i < fireballTargets.Length; i++)
            {
                fireBalls[i] = Instantiate(fireBall, fireballSource.position, Quaternion.identity);
                Vector3 destination = fireballTargets[i].position;
                destination.x += Random.Range(-fireBallDestenationOffsetMax, fireBallDestenationOffsetMax);
                Utility.RotateTowards(destination, fireBalls[i].transform);
                Vector3 distance = destination - fireBalls[i].transform.position;
                float initalYelocity = Random.Range(fireBallInitalYSpeedMin, fireBallInitalYSpeedMax + 5f);
                fireBalls[i].GetComponent<Rigidbody2D>().linearVelocity = Utility.CalcVelocity(distance, initalYelocity, fireBalls[i].GetComponent<Rigidbody2D>().gravityScale);
            }
            sound.FireBall();
            waitTimer = waitBetweenFireBallWavesDuration;
            while (waitTimer > 0)
            {
                waitTimer -= Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
        waitTimer = 0f;
        while (waitTimer < fireBallTimeToTarget)
        {
            waitTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        waitTimer = waitAfterFireBallDuration;
        startFloatPosition = transform.position;
        rb.linearVelocity = Vector2.zero;
        anim.Play("Fly");
        while (waitTimer > 0)
        {
            waitTimer -= Time.fixedDeltaTime;
            Float();
            yield return new WaitForFixedUpdate();
        }
        GoToFlyingState();
        fireBallRoutine = null;
        rb.gravityScale = savedGravity;
    }


    private void GoToFlyingState()
    {
        state = BossState.flying;
        flyTimer = Random.Range(minFlyDuration, maxFlyDuration);
        startFloatPosition = transform.position;
        anim.Play("Fly");
    }

    private void FacePosition(Vector3 target)
    {
        if ((target - transform.position).x > 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = -1;
            transform.localScale = scale;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x = 1;
            transform.localScale = scale;
        }
    }

    private void FaceVelocity()
    {
        if (rb.linearVelocity.x > Mathf.Epsilon)
        {
            Vector3 scale = transform.localScale;
            scale.x = 1;
            transform.localScale = scale;
        }
        else if (rb.linearVelocity.x < -Mathf.Epsilon)
        {
            Vector3 scale = transform.localScale;
            scale.x = -1;
            transform.localScale = scale;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (state == BossState.Dead)
        {
            sound.Fall();
        }
    }
    
    public BossState CurrentState => state;
    public int CurrentHealth => health.currentHealth;
    public bool CanAttack => canAttack;
    public float FlyTimer => flyTimer;

    public bool IsGlideRoutineNull => glideRoutine == null;
    public bool IsSlamRoutineNull => slamRoutine == null;
    public bool IsFireRoutineNull => fireBallRoutine == null;

}
