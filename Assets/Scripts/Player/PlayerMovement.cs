using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField] private float speed;
    [SerializeField] private float jumpPower;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime;
    private float coyoteCounter;

    [Header("Multiple Jumps")]
    [SerializeField] private int extraJumps;
    private int jumpCounter;

    [Header("Wall Mechanics")]
    [SerializeField] private float wallJumpX;
    [SerializeField] private float wallJumpY;
    [SerializeField] private float wallJumpCooldown;
    private float wallJumpCooldownTimer;

    [Header("Wall Mechanics Cooldown after Ledge")]
    [SerializeField] private float wallCooldownAfterLedge = 0.3f;
    private float wallCooldownTimer;

    [Header("Dash Mechanics")]
    [SerializeField] private float dashDistance;
    [SerializeField] private float dashCooldown;
     [SerializeField] private float dashDuration = 0.2f; // Délka dashu (air dash tuning)
    private float dashCooldownTimer;
    private bool isDashing;
    private bool hasAirDashed; // true, pokud už byl dash použit ve vzduchu

    [Header("Ledge Grab")]
    [SerializeField] private float ledgeJumpBackDistance = 0.5f;
    [SerializeField] private float ledgeAnimationDuration = 0.3f; // Duration for animation to play (fallback only)
    [SerializeField] private float ledgeJumpPower = 16f; // Silnější jump
    [SerializeField] private float ledgeHangOffsetY = 0.2f; // Menší offset - blíže k ledge
    [SerializeField] private float ledgeSnapHorizontalOffset = 0.05f; // Ještě přesnější snap
    [SerializeField] private float ledgeHorizontalOffset = 0.1f; // Menší horizontal offset - blíže ke zdi
    [SerializeField] private float ledgeVerticalOffset = 0.2f; // Menší vertical offset - výše na ledge
    [SerializeField] private LedgeHitbox ledgeHitbox;

    [SerializeField] private float ledgeGrabCooldown = 0.2f; // Kratší cooldown
    private float ledgeGrabCooldownTimer;

    private bool isGrabbingLedge;
    public bool IsGrabbingLedge => isGrabbingLedge;
    public bool IsOnWall => onWall();
    public bool IsDashing => isDashing;
    public bool IsClimbingLedge => isClimbingLedge;

    private bool hasSnapped;
    private Vector2 ledgePos;
    private bool isClimbingLedge; // Flag to prevent grounded override during climb animation

    [Header("Step-Up Mechanism")]
    [SerializeField] private float stepHeight = 0.3f; // Maximální výška schodu
    [SerializeField] private float stepCheckDistance = 0.1f; // Vzdálenost pro kontrolu schodu

    [Header("Improved Jump Mechanics")]
    [SerializeField] private float fallGravityMultiplier = 2.5f; // Rychlejší pád
    [SerializeField] private float lowJumpMultiplier = 2f; // Rychlejší pád při krátkém stisku
    [SerializeField] private float jumpBufferTime = 0.1f; // Buffer pro skok
    [SerializeField] private float maxJumpTime = 0.35f; // Maximální doba držení skoku
    [SerializeField] private float maxFallSpeed = 15f; // Maximální rychlost pádu
    [SerializeField] private AudioClip wallAttachSound;
    [SerializeField] private AudioClip[] wallAttachSounds;

    // --- Acceleration/Deceleration ---
    [Header("Acceleration")]
    [SerializeField] private float groundAcceleration = 60f;
    [SerializeField] private float groundDeceleration = 60f;
    [SerializeField] private float airAcceleration = 30f;
    [SerializeField] private float airDeceleration = 30f;

    // --- Apex Assist ---
    [Header("Apex Assist")]
    [SerializeField] private float apexThreshold = 0.3f; // Rychlostní práh poblíž vrcholu skoku
    [SerializeField] private float apexGravityMultiplier = 0.8f; // Nižší gravitace pro "hang time"
    private float jumpBufferCounter;
    private float jumpTimeCounter;
    private bool isJumping;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Collision Check Tuning")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Vector2 groundCheckSizeScale = new Vector2(0.9f, 0.1f);
    [SerializeField] private float groundCheckOffsetY = 0.05f;
    [SerializeField] private float wallCheckDistance = 0.1f;
    [SerializeField] private Vector2 wallCheckSizeScale = new Vector2(0.8f, 0.9f);
    [SerializeField] private Vector2 wallCheckOffset = Vector2.zero;
    [SerializeField] private float sideNormalXThreshold = 0.8f;
    [SerializeField] private float sideNormalYMax = 0.5f;
    [SerializeField] private float slidingDownVyThreshold = -0.05f;

    [Header("Wall Stability")]
    [SerializeField] private float wallCoyoteTime = 0.08f; // drž onWall chvíli po ztrátě kontaktu
    [SerializeField] private float wallDetachInputThreshold = 0.3f; // práh pro "odtlačování" od zdi
    [SerializeField] private float wallReattachDelay = 0.12f; // po wall jumpu dočasně nechytej zdi

    [Header("Wall Slide Reset")]
    [SerializeField] private float wallSlideResetTime = 0.2f; // krátká lhůta bez slide po znovu-připojení
    private float lastWallAttachTime = -999f;

    [Header("Sounds")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip ledgeSound;
    [SerializeField] private AudioSource ledgeAudioSource;

    //tohle jsem pridal ja kdyby to bylo blby tka to smaz - serialized field pro zvuk dopadu a prah (3s) pro prehrani
    [SerializeField] private AudioClip fallLandingSound; //tohle jsem pridal ja kdyby to bylo blby tka to smaz
    [SerializeField] private float fallSoundThreshold = 3f; //tohle jsem pridal ja kdyby to bylo blby tka to smaz

    //tohle jsem pridal ja kdyby to bylo blby tka to smaz - serialized field pro double-jump zvuk (specificky pro "ten druhej")
    [SerializeField] private AudioClip doubleJumpSound; //tohle jsem pridal ja kdyby to bylo blby tka to smaz

    private Rigidbody2D body;
    private Animator anim;
    private BoxCollider2D boxCollider;
    private float horizontalInput;
    private Health health;
    private Vector3 initialPosition;

    private float defaultGravityScale;
    private float lastFacingDirection = 1f;
    private bool prevOnWall = false;

    // Wall stability state
    private float lastWallTouchTime = -999f;
    private float lastWallNormalX = 0f;
    private float lastWallJumpTime = -999f;

    //tohle jsem pridal ja kdyby to bylo blby tka to smaz - interní timer pro sledování pádu
    private float fallTimer = 0f; //tohle jsem pridal ja kdyby to bylo blby tka to smaz

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        health = GetComponent<Health>();
        initialPosition = transform.position;
        defaultGravityScale = body.gravityScale;

        //tohle jsem pridal ja kdyby to bylo blby tka to smaz - fallback: vytvoř AudioSource pokud není přiřazený (usnadní používání)
        if (ledgeAudioSource == null)
        {
            ledgeAudioSource = gameObject.AddComponent<AudioSource>();
            ledgeAudioSource.playOnAwake = false;
        }
    }

    // @SFX:MovementUpdate
    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.R))
        {
            Vector3 dest = health != null ? health.RespawnPoint : initialPosition;
            transform.position = dest;
            body.velocity = Vector2.zero;
        }

        // Flip player – pouze pokud NEjsme na ledge
        if (!isGrabbingLedge)
        {
            if (horizontalInput > 0.01f)
                transform.localScale = Vector3.one;
            else if (horizontalInput < -0.01f)
                transform.localScale = new Vector3(-1, 1, 1);

            lastFacingDirection = Mathf.Sign(transform.localScale.x);
        }
        else
        {
            // Uvolnit ledge grab pokud hráč zmáčkne opačný směr
            if (horizontalInput != 0 && Mathf.Sign(horizontalInput) != lastFacingDirection)
            {
                ReleaseLedge();
            }
        }

        // Animator params
        anim.SetBool("run", horizontalInput != 0);
        if (!isClimbingLedge)
        {
            anim.SetBool("grounded", isGrounded());
        }
        anim.SetBool("dashing", isDashing);
        bool onWallNow = onWall() && !isGrabbingLedge && !isClimbingLedge;
        anim.SetBool("onwall", onWallNow);
        if (onWallNow && !prevOnWall && !isGrabbingLedge)
        {
            if (SoundManager.instance != null)
            {
                if (wallAttachSounds != null && wallAttachSounds.Length > 0)
                    SoundManager.instance.PlayOneOf(wallAttachSounds);
                else if (wallAttachSound != null)
                    SoundManager.instance.PlaySound(wallAttachSound);
            }
        }
        prevOnWall = onWallNow;

        //tohle jsem pridal ja kdyby to bylo blby tka to smaz
        // sledování délky pádu; pokud hráč byl ve stavu pádu (vert. rychlost dolů) a strávil v tom stavu >= fallSoundThreshold sekund,
        // tak se při dopadu přehraje fallLandingSound.
        if (!isGrabbingLedge && !isClimbingLedge)
        {
            // považujeme za "pád" pouze pokud máme zápornou vertikální rychlost (padáme dolů)
            if (!isGrounded() && body.velocity.y < -0.1f)
            {
                fallTimer += Time.deltaTime;
            }
            else if (isGrounded() && fallTimer > 0f)
            {
                if (fallTimer >= fallSoundThreshold)
                {
                    if (ledgeAudioSource != null && fallLandingSound != null)
                    {
                        ledgeAudioSource.PlayOneShot(fallLandingSound);
                    }
                }
                // reset timer po dopadu bez ohledu na to, zda jsme zvuk přehráli
                fallTimer = 0f;
            }

            // pokud jsme ve vzduchu, ale nepadáme (např. stoupáme), tak resetujeme timer
            if (!isGrounded() && body.velocity.y >= -0.1f)
            {
                fallTimer = 0f;
            }
        }
        //tohle jsem pridal ja kdyby to bylo blby tka to smaz

        // --- LEDGE GRAB CHECK ---
        // Debug.Log($"Ledge Check - Grounded: {isGrounded()}, OnWall: {onWall()}, IsGrabbing: {isGrabbingLedge}, CanGrab: {ledgeHitbox.canGrab}, HasSnapped: {hasSnapped}, Cooldown: {ledgeGrabCooldownTimer}");
        
        if (!isGrounded() && !onWall() && !isGrabbingLedge && ledgeHitbox.canGrab &&
            !hasSnapped && ledgeGrabCooldownTimer <= 0)
        {
            // Debug.Log("Starting ledge grab!");
            StartLedgeGrab(ledgeHitbox.ledgePosition);
        }

        if (isGrabbingLedge)
        {
            body.velocity = Vector2.zero;
            body.gravityScale = 0;
            
            // Don't set ledgeGrab to true during climb animation
            if (!isClimbingLedge)
            {
                anim.SetBool("ledgeGrab", true);
            }

            // Snap player to a precise, consistent position on the ledge
            if (!hasSnapped)
            {
                float direction = Mathf.Sign(lastFacingDirection);
                
                // Use adjustable offsets for fine-tuning the ledge position
                // This ensures the character is always at the exact same position relative to the ledge
                Vector2 snapPosition = new Vector2(
                    ledgeHitbox.ledgePosition.x - direction * ledgeHorizontalOffset, 
                    ledgeHitbox.ledgePosition.y - ledgeVerticalOffset
                );
                
                // Vylepšená safety kontrola pro zabránění clipování
                // Kontrola více pozic kolem snap pozice
                bool positionSafe = true;
                Vector2[] checkPositions = {
                    snapPosition,
                    snapPosition + Vector2.left * direction * 0.1f,
                    snapPosition + Vector2.right * direction * 0.1f,
                    snapPosition + Vector2.up * 0.1f,
                    snapPosition + Vector2.down * 0.1f
                };
                
                foreach (Vector2 checkPos in checkPositions)
                {
                    // Kontrola proti ground i wall objektům
                    Collider2D hitCollider = Physics2D.OverlapCircle(checkPos, 0.08f, groundLayer | wallLayer);
                    if (hitCollider != null)
                    {
                        positionSafe = false;
                        break;
                    }
                }
                
                // Pokud pozice není bezpečná, najdi nejbližší bezpečnou pozici
                if (!positionSafe)
                {
                    for (float offset = 0.1f; offset <= 0.5f; offset += 0.1f)
                    {
                        Vector2 safePos = new Vector2(
                            snapPosition.x - direction * offset,
                            snapPosition.y
                        );
                        
                        Collider2D testCollider = Physics2D.OverlapCircle(safePos, 0.08f, groundLayer | wallLayer);
                        if (testCollider == null)
                        {
                            snapPosition = safePos;
                            break;
                        }
                    }
                }
                
                transform.position = snapPosition;
                hasSnapped = true;
            }

            // Climb, jump, drop
            if (Input.GetKeyDown(KeyCode.W))
                StartCoroutine(LedgeClimb());

            if (Input.GetKeyDown(KeyCode.Space) && !isClimbingLedge)
                LedgeJump();

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                ReleaseLedge();

            if (!ledgeHitbox.canGrab)
            {
                // Debug.Log("Releasing ledge because canGrab became false!");
                ReleaseLedge();
            }

            return; // blokuje ostatní pohyb
        }

        // Handle jumping with buffer and variable height
        if (Input.GetKeyDown(KeyCode.Space) && !isClimbingLedge)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Jump buffer logic - včetně wall jump
        if (jumpBufferCounter > 0 && (isGrounded() || coyoteCounter > 0 || jumpCounter > 0 || onWall()) && !isClimbingLedge)
        {
            Jump();
            jumpBufferCounter = 0;
        }

        // Variable jump height - kratší skok při uvolnění tlačítka
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (body.velocity.y > 0)
            {
                body.velocity = new Vector2(body.velocity.x, body.velocity.y * 0.5f);
            }
            isJumping = false;
        }

        // Pokračování v skoku při držení tlačítka
        if (Input.GetKey(KeyCode.Space) && isJumping && jumpTimeCounter > 0)
        {
            // Postupně snižuj sílu skoku pro plynulejší pocit
            float jumpMultiplier = jumpTimeCounter / maxJumpTime;
            jumpTimeCounter -= Time.deltaTime;
        }
        else if (isJumping)
        {
            isJumping = false;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0)
            Dash();

        // Wall slide reset grace: krátce po reattachu neklouzej
        if (onWall() && !isGrounded() && (Time.time - lastWallAttachTime) <= wallSlideResetTime)
        {
            body.gravityScale = 0f;
            if (body.velocity.y < 0)
                body.velocity = new Vector2(body.velocity.x, 0f);
        }
        else
        {
            // Dynamic gravity for better jump feel (with Apex Assist)
            if (!isGrounded() && Mathf.Abs(body.velocity.y) < apexThreshold)
            {
                body.gravityScale = defaultGravityScale * apexGravityMultiplier;
            }
            else if (body.velocity.y < 0)
            {
                // Rychlejší pád
                body.gravityScale = defaultGravityScale * fallGravityMultiplier;
            }
            else if (body.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
            {
                // Rychlejší pád při uvolnění tlačítka během vzletu
                body.gravityScale = defaultGravityScale * lowJumpMultiplier;
            }
            else
            {
                // Normální gravitace během vzletu při držení tlačítka
                body.gravityScale = defaultGravityScale;
            }
        }

        // Omezení maximální rychlosti pádu
        if (body.velocity.y < -maxFallSpeed)
        {
            body.velocity = new Vector2(body.velocity.x, -maxFallSpeed);
        }

        if (!isDashing)
        {
            // Hladká akcelerace/decelerace pro horizontální pohyb
            float currentVelX = body.velocity.x;
            float targetSpeed = horizontalInput * speed;
            bool hasInput = Mathf.Abs(horizontalInput) > 0.01f;
            float accelRate = isGrounded() 
                ? (hasInput ? groundAcceleration : groundDeceleration)
                : (hasInput ? airAcceleration : airDeceleration);
            float newVelX = Mathf.MoveTowards(currentVelX, targetSpeed, accelRate * Time.deltaTime);

            Vector2 targetVelocity = new Vector2(newVelX, body.velocity.y);

            // Step-up kontrola při pohybu po zemi
            if (hasInput && isGrounded())
            {
                targetVelocity = HandleStepUp(targetVelocity);
            }

            body.velocity = targetVelocity;

            if (isGrounded())
            {
                coyoteCounter = coyoteTime;
                jumpCounter = extraJumps;
                hasAirDashed = false; // reset povolení air dash po kontaktu se zemí
            }
            else
            {
                coyoteCounter -= Time.deltaTime;
            }

            // Resetovat doublejump při kontaktu se zdí (wall slide)
            if (onWall() && !isGrounded())
            {
                jumpCounter = extraJumps;
            }
        }

        dashCooldownTimer -= Time.deltaTime;

        if (wallJumpCooldownTimer > 0)
            wallJumpCooldownTimer -= Time.deltaTime;

        if (ledgeGrabCooldownTimer > 0)
            ledgeGrabCooldownTimer -= Time.deltaTime;

        if (wallCooldownTimer > 0)
            wallCooldownTimer -= Time.deltaTime;
    }

    // @SFX:Jump
    private void Jump()
    {
        // Check if SoundManager exists before trying to play sound
        if (SoundManager.instance != null && jumpSound != null)
            SoundManager.instance.PlaySound(jumpSound);

        if (onWall() && wallJumpCooldownTimer <= 0 && wallCooldownTimer <= 0)
            WallJump();
        else if (isGrounded() || coyoteCounter > 0)
        {
            body.velocity = new Vector2(body.velocity.x, jumpPower);
            isJumping = true;
            jumpTimeCounter = maxJumpTime;
            coyoteCounter = 0;
        }
        else if (jumpCounter > 0)
        {
            //tohle jsem pridal ja kdyby to bylo blby tka to smaz - přehraj speciální zvuk pro "double jump" (ten druhej)
            if (doubleJumpSound != null)
            {
                if (SoundManager.instance != null)
                    SoundManager.instance.PlaySound(doubleJumpSound);
                else if (ledgeAudioSource != null)
                    ledgeAudioSource.PlayOneShot(doubleJumpSound);
            }

            body.velocity = new Vector2(body.velocity.x, jumpPower);
            jumpCounter--;
            isJumping = true;
            jumpTimeCounter = maxJumpTime;
            coyoteCounter = 0;

            if (anim != null)
            {
                anim.ResetTrigger("jump");
                anim.SetTrigger("jump");
                var st = anim.GetCurrentAnimatorStateInfo(0);
                anim.Play(st.fullPathHash, 0, 0f);
            }
            return;
        }

        if (anim != null)
        {
            anim.ResetTrigger("jump");
            anim.SetTrigger("jump");
        }
    }

    // @SFX:WallJump
    private void WallJump()
    {
        if (wallCooldownTimer > 0) return; // nelze během cooldownu

        float wallDirection = -Mathf.Sign(transform.localScale.x);
        body.velocity = new Vector2(wallDirection * wallJumpX, wallJumpY);
        wallJumpCooldownTimer = wallJumpCooldown;
        
        // Reset jump variables for wall jump
        isJumping = true;
        jumpTimeCounter = maxJumpTime;
        jumpBufferCounter = 0;

        // Prevent immediate reattach to any wall
        lastWallJumpTime = Time.time;
        
        StartCoroutine(DisableInputTemporarily(0.2f));
        
        // Add null check for animator
        if (anim != null)
            anim.SetTrigger("jump");
    }

    // @SFX:ControlLock
    private IEnumerator DisableInputTemporarily(float duration)
    {
        isDashing = true;
        yield return new WaitForSeconds(duration);
        isDashing = false;
    }

    // @SFX:DashStart
    private void Dash()
    {
        // Zamez opakovanému air dashu během jedné vzdušné fáze
        if (!isGrounded() && hasAirDashed)
            return;

         if (SoundManager.instance != null && dashSound != null)
             SoundManager.instance.PlaySound(dashSound);
         isDashing = true;
         anim.SetBool("dashing", true);
         dashCooldownTimer = dashCooldown;
         anim.SetTrigger("dash");
 
         Vector2 dashDirection = new Vector2(transform.localScale.x, 0).normalized;
 
         // Potlačit gravitaci při air dash
         if (!isGrounded())
             body.gravityScale = 0f;
 
         // Pokud jsme ve vzduchu, označ, že dash byl použit v této vzdušné fázi
         if (!isGrounded())
             hasAirDashed = true;
 
         // Momentum-preserving pouze pokud se hráč pohybuje nahoru (ve vzduchu)
         float preserveVy = (!isGrounded() && body.velocity.y > 0f) ? body.velocity.y : 0f;
         body.velocity = new Vector2(dashDirection.x * dashDistance, preserveVy);
 
         Invoke(nameof(EndDash), dashDuration);
    }

    // @SFX:DashEnd
    private void EndDash()
    {
        isDashing = false;
        anim.SetBool("dashing", false);
        body.gravityScale = defaultGravityScale; // obnovit gravitaci po dashi
    }

    // --- LEDGE FUNCTIONS ---
    // @SFX:LedgeGrab
   private void StartLedgeGrab(Vector2 pos)
{
    isGrabbingLedge = true;
    ledgePos = pos;
    hasSnapped = false;

    // přehrání zvuku ledge grab
    if (ledgeAudioSource != null && ledgeSound != null)
    {
        ledgeAudioSource.PlayOneShot(ledgeSound);
    }
}

    // @SFX:LedgeClimb
    private IEnumerator LedgeClimb()
    {
        // Debug.Log("Starting LedgeClimb coroutine");
        
        // Set climbing flag to prevent grounded override
        isClimbingLedge = true;
        
        // Set animator parameter to prevent jump animation during climb
        anim.SetBool("isClimbing", true);
        
        // Check current animator state before making changes
        // Debug.Log($"Initial state: {anim.GetCurrentAnimatorStateInfo(0).IsName("LedgeGrab")}");
        // Debug.Log($"Initial state name hash: {anim.GetCurrentAnimatorStateInfo(0).shortNameHash}");
        
        // Exit ledgeGrab state but keep grounded true to prevent airborne animation
        anim.SetBool("ledgeGrab", false);
        // Don't set grounded to false - this would trigger airborne animation
        // Debug.Log("Set ledgeGrab to false, keeping grounded state");
        
        // Trigger the climb animation
        anim.SetTrigger("ledgeClimb");
        // Debug.Log("Triggered ledgeClimb animation");
        
        // Check if trigger was set (Note: triggers are consumed immediately, so this might not work)
        // Debug.Log($"Checking animator parameters after trigger");
        
        // Force animator update
        anim.Update(0f);
        yield return null;
        
        // Check state after trigger
        // Debug.Log($"State after trigger: {anim.GetCurrentAnimatorStateInfo(0).IsName("Rattus-LedgeUp")}");
        // Debug.Log($"State hash after trigger: {anim.GetCurrentAnimatorStateInfo(0).shortNameHash}");
        // Debug.Log($"Current state full name: {anim.GetCurrentAnimatorStateInfo(0).fullPathHash}");
        
        // Debug.Log("Using simple timer approach - animation should play now");
        
        // Wait for the animation duration (this allows the animation to play)
        yield return new WaitForSeconds(ledgeAnimationDuration);
        
        // Debug.Log("Animation time completed, moving character");
        
        // Move the character after animation time
        transform.position = new Vector2(ledgePos.x, ledgePos.y + 1f);
        isGrabbingLedge = false;
        body.gravityScale = defaultGravityScale;
        
        // Reset climbing flag and restore normal grounded behavior
        isClimbingLedge = false;
        anim.SetBool("grounded", isGrounded());
        
        // Reset animator climbing parameter
        anim.SetBool("isClimbing", false);

        hasSnapped = false;
        ledgeGrabCooldownTimer = ledgeGrabCooldown;
        wallCooldownTimer = wallCooldownAfterLedge;

        ledgeHitbox.ResetLedge();
        // Debug.Log("LedgeClimb completed");
    }

    // @SFX:LedgeJump
    private void LedgeJump()
    {
        isGrabbingLedge = false;
        body.gravityScale = defaultGravityScale;
        anim.SetBool("ledgeGrab", false);

        hasSnapped = false;

        float pushDirection = -Mathf.Sign(transform.localScale.x);
        transform.position = new Vector2(transform.position.x + pushDirection * ledgeJumpBackDistance, transform.position.y);

        body.velocity = new Vector2(body.velocity.x, ledgeJumpPower);
        anim.SetTrigger("jump");

        ledgeGrabCooldownTimer = ledgeGrabCooldown;
        wallCooldownTimer = wallCooldownAfterLedge;

        ledgeHitbox.ResetLedge(); // 🔹 odemkneme
    }

    // @SFX:LedgeRelease
    private void ReleaseLedge()
    {
        isGrabbingLedge = false;
        body.gravityScale = defaultGravityScale;
        anim.SetBool("ledgeGrab", false);

        hasSnapped = false;

        ledgeGrabCooldownTimer = ledgeGrabCooldown;
        wallCooldownTimer = wallCooldownAfterLedge;

        ledgeHitbox.ResetLedge(); // 🔹 odemkneme
    }

    // --- STEP-UP MECHANISM ---
    // @SFX:StepUp
    private Vector2 HandleStepUp(Vector2 targetVelocity)
    {
        float moveDirection = Mathf.Sign(horizontalInput);
        
        // Zkontroluj, zda je před hráčem překážka na úrovni nohou (ground nebo wall)
        Vector2 frontCheck = new Vector2(
            boxCollider.bounds.center.x + (boxCollider.bounds.size.x * 0.5f + stepCheckDistance) * moveDirection,
            boxCollider.bounds.center.y - boxCollider.bounds.size.y * 0.3f
        );
        
        RaycastHit2D frontHit = Physics2D.Raycast(frontCheck, Vector2.right * moveDirection, stepCheckDistance, groundLayer | wallLayer);
        
        if (frontHit.collider != null)
        {
            // Zkontroluj, zda je nad překážkou volné místo pro step-up
            Vector2 stepUpCheck = new Vector2(
                frontCheck.x + stepCheckDistance * moveDirection,
                boxCollider.bounds.center.y + stepHeight
            );
            
            RaycastHit2D stepUpHit = Physics2D.Raycast(stepUpCheck, Vector2.down, stepHeight + 0.1f, groundLayer | wallLayer);
            
            if (stepUpHit.collider != null)
            {
                float stepUpHeight = stepUpCheck.y - stepUpHit.point.y;
                
                // Pokud je schod dostatečně malý, automaticky ho překonej
                if (stepUpHeight <= stepHeight && stepUpHeight > 0.05f)
                {
                    // Zkontroluj, zda je nad step-up pozicí dostatek místa pro hráče
                    Vector2 headCheck = new Vector2(
                        stepUpHit.point.x,
                        stepUpHit.point.y + boxCollider.bounds.size.y
                    );
                    
                    Collider2D headCollision = Physics2D.OverlapCircle(headCheck, 0.1f, groundLayer | wallLayer);
                    
                    if (headCollision == null)
                    {
                        // Proveď step-up - pozvedni hráče na schod
                        transform.position = new Vector2(
                            transform.position.x,
                            stepUpHit.point.y + boxCollider.bounds.size.y * 0.5f + 0.05f
                        );
                        
                        // Zachovej horizontální rychlost
                        return targetVelocity;
                    }
                }
            }
        }
        
        return targetVelocity;
    }

    // --- COLLISION CHECKS ---
    // @SFX:GroundCheck
    public bool isGrounded()
    {
        // Strict ground check: downward-only cast with normal check (serialized tuning)
        float checkDistance = groundCheckDistance;
        Vector2 feetCenter = new Vector2(boxCollider.bounds.center.x, boxCollider.bounds.min.y + groundCheckOffsetY);
        Vector2 checkSize = new Vector2(
            boxCollider.bounds.size.x * groundCheckSizeScale.x,
            boxCollider.bounds.size.y * groundCheckSizeScale.y
        );

        RaycastHit2D groundHit = Physics2D.BoxCast(
            feetCenter,
            checkSize,
            0,
            Vector2.down,
            checkDistance,
            groundLayer
        );

        bool strictGround = groundHit.collider != null && groundHit.normal.y > 0.5f;
        if (strictGround)
            return true;

        // Allow wall-under-player to count as ground only when not sliding down
        bool slidingDown = body.velocity.y < slidingDownVyThreshold; // wallslide/fall heuristic
        if (!slidingDown)
        {
            RaycastHit2D wallBelowHit = Physics2D.BoxCast(
                feetCenter,
                checkSize,
                0,
                Vector2.down,
                checkDistance,
                wallLayer
            );
            if (wallBelowHit.collider != null && wallBelowHit.normal.y > 0.5f)
                return true;
        }

        return false;
    }

    // @SFX:WallCheck
    private bool onWall()
    {
        // Pokud je aktivní cooldown po ledge akci, ignoruj zdi
        if (wallCooldownTimer > 0)
            return false;

        // Side wall check: horizontal cast with normal filter (serialized tuning)
        Vector2 sideCheckSize = new Vector2(
            boxCollider.bounds.size.x * wallCheckSizeScale.x,
            boxCollider.bounds.size.y * wallCheckSizeScale.y
        );
        Vector2 sideCheckCenter = boxCollider.bounds.center + (Vector3)wallCheckOffset;
        Vector2 dir = new Vector2(Mathf.Sign(transform.localScale.x), 0f);

        // Block reattach shortly after wall jump
        if ((Time.time - lastWallJumpTime) <= wallReattachDelay)
            return false;

        RaycastHit2D wallHit = Physics2D.BoxCast(
            sideCheckCenter,
            sideCheckSize,
            0,
            dir,
            wallCheckDistance,
            wallLayer
        );

        bool hasContact = wallHit.collider != null;

        // Ensure this is a side contact (not a floor/ledge): horizontal normal dominant
        bool sideSurface = hasContact && Mathf.Abs(wallHit.normal.x) > sideNormalXThreshold && Mathf.Abs(wallHit.normal.y) < sideNormalYMax;

        // Ignore wall if strictly grounded below (use serialized ground check tuning)
        float checkDistance = groundCheckDistance;
        Vector2 feetCenter = new Vector2(boxCollider.bounds.center.x, boxCollider.bounds.min.y + groundCheckOffsetY);
        Vector2 checkSize = new Vector2(
            boxCollider.bounds.size.x * groundCheckSizeScale.x,
            boxCollider.bounds.size.y * groundCheckSizeScale.y
        );
        RaycastHit2D groundHit = Physics2D.BoxCast(
            feetCenter,
            checkSize,
            0,
            Vector2.down,
            checkDistance,
            groundLayer
        );
        bool groundedStrict = groundHit.collider != null && groundHit.normal.y > 0.5f;

        // Determine if player is pushing away from wall (relative to wall side)
        // Use current hit normal if present, otherwise last known wall normal during coyote
        float wallNormalX = sideSurface ? wallHit.normal.x : lastWallNormalX;
        bool hasMoveInput = Mathf.Abs(horizontalInput) > 0.01f;
        bool pushingAway = hasMoveInput && (horizontalInput * wallNormalX) > wallDetachInputThreshold;
        bool slidingDown = body.velocity.y < slidingDownVyThreshold;

        // Contact handling: drop onWall immediately when pushing away (even with contact)
        if (sideSurface && !groundedStrict)
        {
            if (pushingAway)
            {
                // cancel coyote when actively detaching
                lastWallTouchTime = -999f;
                lastWallNormalX = wallHit.normal.x;
                return false;
            }
            lastWallTouchTime = Time.time;
            lastWallNormalX = wallHit.normal.x;
            // záznam času reattachu pro reset slide
            lastWallAttachTime = Time.time;
            return true;
        }

        // Grace period: keep onWall shortly after losing contact while sliding down
        // BUT cancel immediately if player is actively pushing away from the last wall side
        if (!groundedStrict && slidingDown && !pushingAway && (Time.time - lastWallTouchTime) <= wallCoyoteTime)
        {
            return true;
        }

        return false;
    }

    // @SFX:AttackReady
    public bool canAttack()
    {
        return horizontalInput == 0 && isGrounded() && !onWall();
    }

    // --- GIZMOS ---
    // @SFX:DebugGizmos
    private void OnDrawGizmosSelected()
    {
        // Bezpečná reference na box collider i v editoru
        var bc = boxCollider != null ? boxCollider : GetComponent<BoxCollider2D>();
        if (bc == null) return;

        // --- GROUND CHECK GIZMO --- odpovídá isGrounded() s tunable parametry
        Vector2 feetCenter = new Vector2(bc.bounds.center.x, bc.bounds.min.y + groundCheckOffsetY);
        Vector2 groundCheckSize = new Vector2(
            bc.bounds.size.x * groundCheckSizeScale.x,
            bc.bounds.size.y * groundCheckSizeScale.y
        );

        // start box
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(feetCenter, groundCheckSize);
        Gizmos.color = new Color(0f, 0.8f, 0f, 1f);
        Gizmos.DrawWireCube(feetCenter, groundCheckSize);

        // end box + směr
        Vector2 feetEnd = feetCenter + Vector2.down * groundCheckDistance;
        Gizmos.color = new Color(0f, 0.6f, 0f, 0.2f);
        Gizmos.DrawCube(feetEnd, groundCheckSize);
        Gizmos.color = new Color(0f, 0.6f, 0f, 1f);
        Gizmos.DrawWireCube(feetEnd, groundCheckSize);
        Gizmos.DrawLine(feetCenter, feetEnd);

        // --- WALL CHECK GIZMO --- odpovídá onWall() s tunable parametry
        Vector2 wallCheckSize = new Vector2(
            bc.bounds.size.x * wallCheckSizeScale.x,
            bc.bounds.size.y * wallCheckSizeScale.y
        );
        Vector2 wallCheckCenter = bc.bounds.center + (Vector3)wallCheckOffset;
        Vector2 dir = new Vector2(Mathf.Sign(transform.localScale.x), 0f);

        // start box
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawCube(wallCheckCenter, wallCheckSize);
        Gizmos.color = new Color(0f, 1f, 1f, 1f);
        Gizmos.DrawWireCube(wallCheckCenter, wallCheckSize);

        // end box + směr
        Vector2 wallEnd = wallCheckCenter + dir * wallCheckDistance;
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.2f);
        Gizmos.DrawCube(wallEnd, wallCheckSize);
        Gizmos.color = new Color(0f, 0.7f, 1f, 1f);
        Gizmos.DrawWireCube(wallEnd, wallCheckSize);
        Gizmos.DrawLine(wallCheckCenter, wallEnd);

        // --- LEDGE SNAP GIZMO --- (ponecháno)
        if (ledgeHitbox == null) return;

        // poslední známá pozice ledge
        Vector2 basePos = ledgeHitbox.ledgePosition;
        float direction = Mathf.Sign(transform.localScale.x) * -1;
        Vector2 snapPosition = new Vector2(
            basePos.x + direction * ledgeSnapHorizontalOffset,
            basePos.y - ledgeHangOffsetY
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(snapPosition, 0.1f);

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawCube(snapPosition, new Vector3(0.3f, 0.3f, 0.3f));
    }
}
