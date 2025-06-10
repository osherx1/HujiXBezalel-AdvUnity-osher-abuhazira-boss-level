using System.Collections;
using UnityEditor.Playables;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float jumpSpeed = 5f;
    public float maxJumpLength = 0.5f;
    public float minJumpLength = 0.1f;
    public float dashSpeed = 15f;
    public float dashLength = 0.3f;
    public float dashCoolDown = 0.2f;
    public float preAttackLength = 0.1f;
    public float attackLength = 0.1f;
    public float postAttackLength = 0.1f;
    public GameObject attackPrefab = null;
    public float attackCoolDown = 0.1f;
    public LayerMask groundLayer = 1;
    public float attackBufferInputLength = 0.1f;
    public float dashBufferInputLength = 0.1f;
    public float jumpBufferInputLength = 0.2f;
    public float maxFallSpeed = 20f;
    public float jumpForgiveLength = 0.06f;
    public float attackKnockBackSpeed = 30f;
    public float attackKnockBackDuration = 0.04f;
    public float getHitDuration = 0.1f;
    public SpriteRenderer spriteRenderer;
    public Material getHitMaterial;
    public float invincibleDuration = 0.3f;
    public GameObject attackUpPrefab = null;
    public GameObject attackDownPrefab = null;
    public float bounceSpeed = 10f;
    public float bounceDuration = 0.2f;
    public GameObject hitEffectGO;
    public float hitEffectDuration = 0.1f;
    public float hitPauseDuration = 0.5f;
    public ParticleSystem getHitEffectParticles;
    public int maxRally = 1;
    public float deadFloatUpSpeed = 2f;
    public PlayerUI playerUI;
    public PlayerSound sound;
    public float stepInterval = 0.15f;
    public float hazardDeathBlackScreenDuration = 0.3f;
    public float gameOverWaitTime = 3f;
    public float rallyDuration = 3f;
    public ParticleSystem dashEffect;
    public ParticleSystem dashEffect2;
    public float deathParticleRate = 500f;

    private Rigidbody2D rb;
    private Collider2D boxCol;
    private GPlayerInputActions input;
    private Animator anim;
    private Health health;
    private Material normalMaterial;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        health = GetComponent<Health>();
        input = new GPlayerInputActions();
        normalMaterial = spriteRenderer.material;

        input.Enable();
    }
    private void Start()
    {
        playerUI.UpdateHealth(health.currentHealth, currentCanRally);
    }
    private void OnEnable()
    {
        input.Player.Jump.performed += Jump_performed;
        input.Player.Jump.canceled += Jump_canceled;
        input.Player.Dash.performed += Dash_performed;
        input.Player.Attack.performed += Attack_performed;
        input.Player.Pause.performed += Pause_performed; ;
        health.OnDamaged += Health_OnDamaged;
        attackDownPrefab.GetComponent<AttackBox>().OnHit += Player_OnHitDown;
        attackPrefab.GetComponent<AttackBox>().OnHit += Player_OnHit;
        attackUpPrefab.GetComponent<AttackBox>().OnHit += Player_OnHit;
    }

    private bool isPaused = false;
    private float savedTimeScale = 0f;
    private void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (isPaused)
        {
            var particlesMain = getHitEffectParticles.main;
            particlesMain.useUnscaledTime = true;
            isPaused = false;
            Time.timeScale = savedTimeScale;
        }
        else
        {
            var particlesMain = getHitEffectParticles.main;
            particlesMain.useUnscaledTime = false;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isPaused = true;
        }
        playerUI.Pause(isPaused);
    }

    private bool isBouncing = false;
    private float bounceTimer = 0f;
    private void Player_OnHitDown(HitInfo info)
    {
        isBouncing = true;
        bounceTimer = bounceDuration;
        hasAirDashed = false;
        Player_OnHit(info);
    }
    private void Player_OnHit(HitInfo info)
    {
        sound.HitSomething();
        var enemyRB = info.target.GetComponent<Collider2D>().attachedRigidbody;
        if (enemyRB != null && enemyRB.CompareTag("Enemy") && currentCanRally > 0)
        {
            sound.Rally();
            health.currentHealth++;
            currentCanRally--;
            playerUI.UpdateHealth(health.currentHealth, currentCanRally);
            if (currentCanRally > maxRally)
            {
                currentCanRally = maxRally;
            }
            if (currentCanRally > health.maxHealth - health.currentHealth)
            {
                currentCanRally = health.maxHealth - health.currentHealth;
            }
        }
        StartCoroutine(HitEffect(info));
    }
    IEnumerator HitEffect(HitInfo info)
    {
        hitEffectGO.transform.position = info.point;
        Vector3 hitDirection = info.direction;
        Utility.RotateTowards(info.point + hitDirection * 5f, hitEffectGO.transform);
        hitEffectGO.SetActive(true);
        hitEffectGO.transform.parent = null;
        float timer = hitEffectDuration;
        while (timer > 0f)
        {
            timer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        hitEffectGO.transform.parent = transform;
        hitEffectGO.SetActive(false);
    }

    private void OnDisable()
    {
        input.Player.Jump.performed -= Jump_performed;
        input.Player.Jump.canceled -= Jump_canceled;
        input.Player.Dash.performed -= Dash_performed;
        input.Player.Attack.performed -= Attack_performed;
        health.OnDamaged -= Health_OnDamaged;
        attackDownPrefab.GetComponent<AttackBox>().OnHit -= Player_OnHitDown;
        attackPrefab.GetComponent<AttackBox>().OnHit -= Player_OnHit;
        attackUpPrefab.GetComponent<AttackBox>().OnHit -= Player_OnHit;
    }

    private bool wantToStopJump = false;
    private void Jump_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        wantToStopJump = true;
    }

    private bool wantToJump = false;
    private void Jump_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        wantToJump = true;
    }

    private bool wantToDash = false;
    private void Dash_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        wantToDash = true;
    }

    public bool wantToAttack = false;
    private void Attack_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        wantToAttack = true;
    }
    private float beingAttackedTimer = 0f;
    private Vector2 knockBackDirection = Vector2.zero;
    private float invincibleTimer = 0f;
    private int currentCanRally = 0;
    private AttackBox hazard;
    private bool isDead = false;
    private float rallyTimer = 0f;
    private void Health_OnDamaged(HitInfo info)
    {
        sound.GetHit();
        if (health.currentHealth == 0)
        {
            var camShake = FindObjectOfType<CameraShake>();
            camShake.shakePower = 0.5f;
            camShake.timeScale = 0.5f;
            camShake.Shake();
            rb.linearVelocity = Vector2.up * deadFloatUpSpeed;
            rb.isKinematic = true;
            if (currentAttackPrefab != null)
            {
                currentAttackPrefab.SetActive(false);
            }
            isDead = true;
            health.SetCanHit(false);
            var emmision = getHitEffectParticles.emission;
            emmision.rateOverTime = new ParticleSystem.MinMaxCurve(deathParticleRate);
            getHitEffectParticles.Play();
            anim.Play("Dead");
            playerUI.UpdateHealth(health.currentHealth, 0);
            currentCanRally = 0;
            StartCoroutine(GameOver());
            if (isDashing)
            {
                dashEffect.Stop(); dashEffect2.Stop();
            }
            return;
        }

        currentCanRally++;
        if (currentCanRally > maxRally)
        {
            currentCanRally = maxRally;
        }
        if (currentCanRally > health.maxHealth - health.currentHealth)
        {
            currentCanRally = health.maxHealth - health.currentHealth;
        }
        rallyTimer = rallyDuration;
        isBeingAttacked = true;
        beingAttackedTimer = 0f;
        knockBackDirection = info.direction;
        knockBackDirection.y = 0.5f;
        if (knockBackDirection.x > 0)
        {
            knockBackDirection.x = 1;
        }
        else
        {
            knockBackDirection.x = -1;
        }
        //knockBackDirection.Normalize();
        spriteRenderer.material = getHitMaterial;
        invincibleTimer = invincibleDuration;
        health.SetCanHit(false);
        playerUI.UpdateHealth(health.currentHealth, currentCanRally);
        if (isAttacking)
        {
            isAttacking = false;
            currentAttackPrefab.SetActive(false);
            attackCoolDownTimer = attackCoolDown;
            anim.Play("Idle");
        }
        if (isDashing)
        {
            anim.Play("Idle");
            isDashing = false;
            dashEffect.Stop(); dashEffect2.Stop();
        }
        else
        {
            savedGravity = rb.gravityScale;
        }
        if (isJumping)
        {
            anim.Play("Idle");
            isJumping = false;
            endJumpOnMin = false;
        }
        if (isBouncing)
        {
            isBouncing = false;
        }
        if (isRunning)
        {
            isRunning = false;
        }
        if (isFalling)
        {
            isFalling = false;
        }
        rb.gravityScale = 0f;

        if (info.damageType == 1)
        {
            knockBackDirection = Vector2.zero;
            hazard = info.attacker;
        }
        else
        {
            hazard = null;
            var camShake = FindObjectOfType<CameraShake>();
            camShake.shakePower = 0.25f;
            camShake.timeScale = 0.6f;
            camShake.Shake();
        }
        StartCoroutine(PauseGameForHit());
    }

    IEnumerator GameOver()
    {
        yield return new WaitForSeconds(gameOverWaitTime);
        BlackScreen.instance.fadeDuration = 0.3f;
        BlackScreen.instance.FadeToBlack();
        yield return new WaitForSecondsRealtime(0.5f);
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainScene");
    }
    IEnumerator PauseGameForHit()
    {
        if (hazard != null)
        {
            BlackScreen.instance.done += FadeDone;
            BlackScreen.instance.fadeDuration = hazardDeathBlackScreenDuration;
            BlackScreen.instance.FadeToBlack();
        }
        Time.timeScale = 0f;
        getHitEffectParticles.Play();
        yield return new WaitForSecondsRealtime(hitPauseDuration);
        yield return new WaitWhile(() => isPaused);
        if (hazard != null)
        {
            yield return new WaitUntil(() => fadeDone);
            transform.position = hazard.GetComponentInParent<Hazard>().respawnPosition.position;
            FindObjectOfType<FollowCamera>().SnapToTarget();
            BlackScreen.instance.FadeFromBlack();
        }
        Time.timeScale = 1f;

    }
    private bool fadeDone = false;
    private void FadeDone()
    {
        fadeDone = true;
    }

    private int moveDir = 0;
    private int lookYDir = 0;
    private float lookPower = 0f;
    private float movePower = 0f;
    private void Update()
    {
        if (isDead || isPaused)
        {
            return;
        }
        float moveDirFloat = input.Player.Move.ReadValue<float>();
        movePower = Mathf.Abs(moveDirFloat);
        float lookYDirFloat = input.Player.Look.ReadValue<float>();
        lookPower = Mathf.Abs(lookYDirFloat);
        if (moveDirFloat > Mathf.Epsilon)
        {
            moveDir = 1;
        }
        else if (moveDirFloat < -Mathf.Epsilon)
        {
            moveDir = -1;
        }
        else
        {
            moveDir = 0;
        }
        if (moveDir != 0)
        {
            lookDir = moveDir;
        }
        else
        {
            lookDir = facingDir;
        }
        if (lookYDirFloat > Mathf.Epsilon)
        {
            lookYDir = 1;
        }
        else if (lookYDirFloat < -Mathf.Epsilon)
        {
            lookYDir = -1;
        }
        else
        {
            lookYDir = 0;
        }
    }
    private bool isJumping = false;
    private bool endJumpOnMin = false;
    private float jumpTimer = 0f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float savedGravity = 0f;
    private float dashCoolDownTimer = 0f;
    private int dashDir = 1;
    private int lookDir = 1;
    private bool isAttacking = false;
    private float attackTimer = 0f;
    private bool jumpWasUsed = false;
    private int facingDir = 1;
    private float attackCoolDownTimer = 0f;
    private float attackBufferedTimer = 0f;
    private float dashBufferedTimer = 0f;
    private float jumpBufferedTimer = 0f;
    private bool endNextJumpOnMin = false;
    private bool isRunning = false;
    private bool isIdle = false;
    private bool isFalling = false;
    private float jumpForgiveTimer = 0f;
    private bool preAttackDone = false;
    private bool attackDone = false;
    private bool isBeingAttacked = false;
    private GameObject currentAttackPrefab = null;
    private float stepTimer = 0f;
    private bool hasAirDashed = false;
    private void FixedUpdate()
    {
    if (isDead)
        return;

    CheckIsOnGround();
    HandleBeingAttacked();
    HandleDashLogic();
    HandleAttackLogic();
    HandleJumpLogic();
    HandleBounceLogic();
    HandleGeneralLogic();
    HandleRunningIdleFallingLogic();
    HandleTimersAndInputReset();
    }
    
    private void HandleBeingAttacked()
    {
        if (!isBeingAttacked)
            return;

        // Determine phase of knockback
        if (beingAttackedTimer < attackKnockBackDuration)
        {
            ApplyKnockback();
        }
        else if (beingAttackedTimer < attackKnockBackDuration + getHitDuration)
        {
            StopAndRestoreGravity();
        }
        else
        {
            EndBeingAttacked();
        }
        anim.Play("Idle");
        beingAttackedTimer += Time.fixedDeltaTime;
    }

    private void ApplyKnockback()
    {
        rb.linearVelocity = knockBackDirection * attackKnockBackSpeed;
    }

    private void StopAndRestoreGravity()
    {
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = savedGravity;
    }

    private void EndBeingAttacked()
    {
        rb.gravityScale = savedGravity;
        isBeingAttacked = false;
    }

    
    private void HandleDashLogic()
    {
        if (isOnGround && hasAirDashed)
            hasAirDashed = false;

        bool canDash = (isOnGround || !hasAirDashed) && !isBeingAttacked && !isDashing && !isAttacking && dashCoolDownTimer <= Mathf.Epsilon;

        if ((wantToDash || dashBufferedTimer > Mathf.Epsilon) && canDash)
        {
            StartDash();
        }
        else if (wantToDash)
        {
            dashBufferedTimer = dashBufferInputLength;
        }

        if (isDashing)
        {
            ContinueDash();
        }
        else
        {
            CooldownDash();
        }
    }

    private void StartDash()
    {
        if (!isOnGround)
            hasAirDashed = true;

        sound.Dash();
        PlayDashEffects(true);
        isDashing = true;
        dashTimer = 0;
        savedGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        dashDir = lookDir;
        dashBufferedTimer = 0;
        anim.Play("Dash");

        StopActionIf(ref isRunning);
        StopActionIf(ref isJumping, ref endJumpOnMin);
        StopActionIf(ref isBouncing);
    }

    private void ContinueDash()
    {
        dashTimer += Time.fixedDeltaTime;
        if (dashTimer >= dashLength)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = savedGravity;
            isDashing = false;
            PlayDashEffects(false);
        }
        else
        {
            dashCoolDownTimer = dashCoolDown;
            rb.linearVelocity = new Vector2(dashSpeed * dashDir, 0);
        }
    }

    private void CooldownDash()
    {
        if (dashCoolDownTimer > 0f)
            dashCoolDownTimer -= Time.fixedDeltaTime;
    }

    private void PlayDashEffects(bool play)
    {
        if (play)
        {
            dashEffect?.Play();
            dashEffect2?.Play();
        }
        else
        {
            dashEffect?.Stop();
            dashEffect2?.Stop();
        }
    }

    private void StopActionIf(ref bool actionFlag, ref bool optionalFlag)
    {
        if (actionFlag)
        {
            actionFlag = false;
            optionalFlag = false;
        }
    }
    private void StopActionIf(ref bool actionFlag)
    {
        if (actionFlag) actionFlag = false;
    }


private void HandleAttackLogic()
{
    bool canAttack = !isBeingAttacked && !isAttacking && !isDashing && attackCoolDownTimer <= Mathf.Epsilon;

    if ((wantToAttack || attackBufferedTimer > Mathf.Epsilon) && canAttack)
    {
        StartAttack();
    }
    else if (wantToAttack)
    {
        attackBufferedTimer = attackBufferInputLength;
    }

    if (isAttacking)
    {
        ContinueAttack();
    }
    else
    {
        if (attackCoolDownTimer > 0f)
            attackCoolDownTimer -= Time.fixedDeltaTime;
    }
}

private void StartAttack()
{
    currentAttackPrefab = ChooseAttackPrefab();
    anim.Play(GetAttackAnimName());
    sound.Attack();
    isAttacking = true;
    attackTimer = 0f;
    attackBufferedTimer = 0;
    preAttackDone = false;
    attackDone = false;
}

private void ContinueAttack()
{
    attackTimer += Time.fixedDeltaTime;

    if (attackTimer < preAttackLength)
        return; // Waiting pre-attack

    if (attackTimer < preAttackLength + attackLength)
    {
        if (!preAttackDone)
        {
            preAttackDone = true;
            SetAttackPrefabActive(true);
        }
    }
    else if (attackTimer < preAttackLength + attackLength + postAttackLength)
    {
        if (!attackDone)
        {
            attackDone = true;
            SetAttackPrefabActive(false);
        }
    }
    else
    {
        EndAttack();
    }
}

private void EndAttack()
{
    SetAttackPrefabActive(false);
    isAttacking = false;
    attackCoolDownTimer = attackCoolDown;

    if (isJumping)
        anim.Play("Jump");
    if (isRunning)
        anim.Play("Walk");
}

private GameObject ChooseAttackPrefab()
{
    if (lookYDir == 1 && lookPower >= movePower)
        return attackUpPrefab;
    if (lookYDir == -1 && lookPower >= movePower && !isOnGround)
        return attackDownPrefab;
    return attackPrefab;
}

private string GetAttackAnimName()
{
    if (lookYDir == 1 && lookPower >= movePower)
        return "AttackUp";
    if (lookYDir == -1 && lookPower >= movePower && !isOnGround)
        return "AttackDown";
    return "Attack";
}

private void SetAttackPrefabActive(bool active)
{
    if (currentAttackPrefab != null)
        currentAttackPrefab.SetActive(active);
}


private void HandleJumpLogic()
{
    if (isOnGround && jumpWasUsed)
        jumpWasUsed = false;

    bool canJump = (isOnGround || jumpForgiveTimer > Mathf.Epsilon)
                   && (wantToJump || jumpBufferedTimer > Mathf.Epsilon)
                   && !isBeingAttacked && !isJumping && !isDashing && !jumpWasUsed;

    if (canJump)
    {
        if (endNextJumpOnMin)
        {
            endNextJumpOnMin = false;
            endJumpOnMin = true;
        }
        sound.Jump();
        jumpForgiveTimer = 0f;
        anim.Play("Jump");
        isJumping = true;
        jumpWasUsed = true;
        jumpTimer = 0f;
        jumpBufferedTimer = 0f;
        if (isRunning) isRunning = false;
    }
    else if (wantToJump)
    {
        jumpBufferedTimer = jumpBufferInputLength;
    }

    if (wantToStopJump && isJumping)
    {
        if (jumpTimer < minJumpLength)
        {
            endJumpOnMin = true;
        }
        else
        {
            StopJump();
        }
    }

    if (isJumping)
    {
        jumpTimer += Time.fixedDeltaTime;
        if (jumpTimer >= maxJumpLength || (jumpTimer >= minJumpLength && endJumpOnMin) || HasHitHead())
        {
            endJumpOnMin = false;
            StopJump();
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
        }
    }
}

private void StopJump()
{
    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
    isJumping = false;
}


private void HandleBounceLogic()
{
    if (!isBouncing)
        return;

    rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceSpeed);
    bounceTimer -= Time.fixedDeltaTime;

    if (bounceTimer <= 0f)
    {
        isBouncing = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
    }
}


private void HandleGeneralLogic()
{
    HandleInvincibility();
    HandleRally();

    // Clamp falling speed
    if (!isDashing && !isBeingAttacked && rb.linearVelocity.y < -maxFallSpeed)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);

    // Horizontal movement
    if (!isDashing && !isBeingAttacked)
        rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
}

private void HandleInvincibility()
{
    if (invincibleTimer > 0)
    {
        invincibleTimer -= Time.fixedDeltaTime;
        if (invincibleTimer < Mathf.Epsilon)
        {
            spriteRenderer.material = normalMaterial;
            health.SetCanHit(true);
        }
    }
}

private void HandleRally()
{
    if (rallyTimer > 0)
    {
        rallyTimer -= Time.fixedDeltaTime;
        if (rallyTimer < Mathf.Epsilon)
        {
            rallyTimer = 0f;
            currentCanRally = 0;
            playerUI.UpdateHealth(health.currentHealth, 0);
        }
    }
}


private void HandleRunningIdleFallingLogic()
{
    if (!isRunning && moveDir != 0 && !isAttacking && !isJumping && isOnGround && !isDashing && !isBeingAttacked)
        StartRunning();
    else if (moveDir == 0)
        isRunning = false;

    HandleLanding();
    HandleIdleAndFalling();
    HandleStepSoundAndSpriteFlip();
}

private void StartRunning()
{
    isRunning = true;
    anim.Play("Walk");
    stepTimer = stepInterval;
    sound.Step();
}

private void HandleLanding()
{
    if (isFalling && isOnGround)
    {
        if (rb.linearVelocity.y < -19f)
            ShakeCameraOnLand();
        sound.Land();
    }
}

private void ShakeCameraOnLand()
{
    var camShake = FindObjectOfType<CameraShake>();
    camShake.shakePower = 0.1f;
    camShake.timeScale = 1.2f;
    camShake.Shake();
}

private void HandleIdleAndFalling()
{
    if (!isRunning && !isDashing && !isAttacking && !isJumping && !isBeingAttacked)
    {
        if (isOnGround)
        {
            if (!isIdle)
            {
                isIdle = true;
                anim.Play("Idle");
            }
        }
        else
        {
            if (!isFalling)
            {
                isFalling = true;
                anim.Play("Falling");
            }
        }
    }
    else
    {
        isIdle = false;
        isFalling = false;
    }
    if (isFalling)
        isIdle = false;

    if (isOnGround)
    {
        isFalling = false;
        jumpForgiveTimer = jumpForgiveLength;
    }
}

private void HandleStepSoundAndSpriteFlip()
{
    if (!isDashing && !isAttacking && !isBeingAttacked)
    {
        if (isRunning)
        {
            if (stepTimer > 0f)
            {
                stepTimer -= Time.fixedDeltaTime;
            }
            else
            {
                stepTimer = stepInterval;
                sound.Step();
            }
        }
        if (facingDir != lookDir)
        {
            FlipSprite();
        }
    }
}

private void FlipSprite()
{
    Vector3 scale = transform.localScale;
    scale.x *= -1;
    transform.localScale = scale;
    facingDir = lookDir;
}


private void HandleTimersAndInputReset()
{
    if (attackBufferedTimer > 0)
        attackBufferedTimer -= Time.fixedDeltaTime;

    if (dashBufferedTimer > 0)
        dashBufferedTimer -= Time.fixedDeltaTime;

    if (jumpBufferedTimer > 0)
    {
        if (wantToStopJump)
            endNextJumpOnMin = true;

        jumpBufferedTimer -= Time.fixedDeltaTime;
        if (jumpBufferedTimer < Mathf.Epsilon)
            endNextJumpOnMin = false;
    }
    if (jumpForgiveTimer > 0)
        jumpForgiveTimer -= Time.fixedDeltaTime;

    // Reset input flags
    wantToJump = false;
    wantToStopJump = false;
    wantToDash = false;
    wantToAttack = false;
}


    private bool HasHitHead()
    {
        Vector2 origin = boxCol.bounds.center + (Vector3.up * (boxCol.bounds.extents.y * 0.75f));
        Vector2 size = new Vector2(boxCol.bounds.size.x * 0.6f, boxCol.bounds.extents.y * 0.45f);
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();
        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask(groundLayer);
        int collisionCount = Physics2D.OverlapBox(origin, size, 0, contactFilter, collisionResult);
        return collisionCount != 0;
    }

    private bool isOnGround = false;
    private Collider2D[] collisionResult = new Collider2D[1];
    private void CheckIsOnGround()
    {
        Vector2 origin = boxCol.bounds.center + (Vector3.down * (boxCol.bounds.extents.y * 0.75f));
        Vector2 size = new Vector2(boxCol.bounds.size.x * 0.8f, boxCol.bounds.extents.y * 0.75f);
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();
        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask(groundLayer);
        int collisionCount = Physics2D.OverlapBox(origin, size, 0, contactFilter, collisionResult);
        if (collisionCount != 0)
        {
            isOnGround = true;
        }
        else
        {
            isOnGround = false;
        }
    }
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        Vector2 origin = col.bounds.center + (Vector3.down * (col.bounds.extents.y * 0.75f));
        Vector2 size = new Vector2(col.bounds.size.x * 0.8f, col.bounds.extents.y * 0.75f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(origin, size);

        origin = col.bounds.center + (Vector3.up * (col.bounds.extents.y * 0.75f));
        size = new Vector2(col.bounds.size.x * 0.6f, col.bounds.extents.y * 0.45f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(origin, size);
    }
}
