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
    [SerializeField] private float wallCooldownAfterLedge = 0.2f;
    private float wallCooldownTimer;

    [Header("Dash Mechanics")]
    [SerializeField] private float dashDistance;
    [SerializeField] private float dashCooldown;
    private float dashCooldownTimer;
    private bool isDashing;

    [Header("Ledge Grab")]
    [SerializeField] private float ledgeJumpBackDistance = 0.5f;
    [SerializeField] private float ledgeClimbDuration = 0.5f;
    [SerializeField] private float ledgeJumpPower = 14f;
    [SerializeField] private float ledgeHangOffsetY = 0.5f;
    [SerializeField] private float ledgeSnapHorizontalOffset = 0.1f;
    [SerializeField] private LedgeHitbox ledgeHitbox;

    [SerializeField] private float ledgeGrabCooldown = 0.3f;
    private float ledgeGrabCooldownTimer;

    private bool isGrabbingLedge;
    private bool hasSnapped;
    private Vector2 ledgePos;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Sounds")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip dashSound;

    private Rigidbody2D body;
    private Animator anim;
    private BoxCollider2D boxCollider;
    private float horizontalInput;

    private float defaultGravityScale;
    private float lastFacingDirection = 1f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        defaultGravityScale = body.gravityScale;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

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
            // Uvolnit ledge grab pokud hráè zmáèkne opaèný smìr
            if (horizontalInput != 0 && Mathf.Sign(horizontalInput) != lastFacingDirection)
            {
                ReleaseLedge();
            }
        }

        // Animator params
        anim.SetBool("run", horizontalInput != 0);
        anim.SetBool("grounded", isGrounded());
        anim.SetBool("onwall", onWall());

        // --- LEDGE GRAB CHECK ---
        if (!isGrounded() && !isGrabbingLedge && ledgeHitbox.canGrab &&
            ledgeHitbox.upperCheck != null && ledgeHitbox.upperCheck.isClear &&
            !hasSnapped && ledgeGrabCooldownTimer <= 0)
        {
            StartLedgeGrab(ledgeHitbox.ledgePosition);
        }

        if (isGrabbingLedge)
        {
            body.velocity = Vector2.zero;
            body.gravityScale = 0;
            anim.SetBool("ledgeGrab", true);

            // Snap hráèe jednou se bezpeèným horizontálním offsetem
            if (!hasSnapped)
            {
                float direction = Mathf.Sign(lastFacingDirection) * -1;
                Vector2 snapPosition = new Vector2(
                    ledgeHitbox.ledgePosition.x + direction * ledgeSnapHorizontalOffset,
                    ledgeHitbox.ledgePosition.y - ledgeHangOffsetY
                );
                transform.position = snapPosition;
                hasSnapped = true;
            }

            // Climb, jump, drop
            if (Input.GetKeyDown(KeyCode.W))
                StartCoroutine(LedgeClimb());

            if (Input.GetKeyDown(KeyCode.Space))
                LedgeJump();

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                ReleaseLedge();

            if (!ledgeHitbox.canGrab)
                ReleaseLedge();

            return; // blokuje ostatní pohyb
        }

        // Handle jumping
        if (Input.GetKeyDown(KeyCode.Space))
            Jump();

        // Handle dashing
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0)
            Dash();

        if (Input.GetKeyUp(KeyCode.Space) && body.velocity.y > 0)
            body.velocity = new Vector2(body.velocity.x, body.velocity.y / 2);

        if (!isDashing)
        {
            body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);

            if (isGrounded())
            {
                coyoteCounter = coyoteTime;
                jumpCounter = extraJumps;
            }
            else
            {
                coyoteCounter -= Time.deltaTime;
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

    private void Jump()
    {
        if (coyoteCounter <= 0 && !onWall() && jumpCounter <= 0) return;

        SoundManager.instance.PlaySound(jumpSound);

        if (onWall() && wallJumpCooldownTimer <= 0 && wallCooldownTimer <= 0)
            WallJump();
        else
        {
            if (isGrounded() || coyoteCounter > 0)
                body.velocity = new Vector2(body.velocity.x, jumpPower);
            else if (jumpCounter > 0)
            {
                body.velocity = new Vector2(body.velocity.x, jumpPower);
                jumpCounter--;
            }

            coyoteCounter = 0;
        }

        anim.SetTrigger("jump");
    }

    private void WallJump()
    {
        if (wallCooldownTimer > 0) return; // nelze bìhem cooldownu

        float wallDirection = -Mathf.Sign(transform.localScale.x);
        body.velocity = new Vector2(wallDirection * wallJumpX, wallJumpY);
        wallJumpCooldownTimer = wallJumpCooldown;
        StartCoroutine(DisableInputTemporarily(0.2f));
        anim.SetTrigger("jump");
    }

    private IEnumerator DisableInputTemporarily(float duration)
    {
        isDashing = true;
        yield return new WaitForSeconds(duration);
        isDashing = false;
    }

    private void Dash()
    {
        SoundManager.instance.PlaySound(dashSound);
        isDashing = true;
        dashCooldownTimer = dashCooldown;
        anim.SetTrigger("dash");

        Vector2 dashDirection = new Vector2(transform.localScale.x, 0).normalized;
        body.velocity = dashDirection * dashDistance;

        Invoke(nameof(EndDash), 0.2f);
    }

    private void EndDash()
    {
        isDashing = false;
    }

    // --- LEDGE FUNCTIONS ---
    private void StartLedgeGrab(Vector2 pos)
    {
        isGrabbingLedge = true;
        ledgePos = pos;
        hasSnapped = false;

        if (ledgeHitbox.upperCheck != null)
            ledgeHitbox.upperCheck.gameObject.SetActive(false);
    }

    private IEnumerator LedgeClimb()
    {
        anim.SetTrigger("ledgeClimb");
        yield return new WaitForSeconds(ledgeClimbDuration);

        transform.position = new Vector2(ledgePos.x, ledgePos.y + 1f);
        isGrabbingLedge = false;
        body.gravityScale = defaultGravityScale;
        anim.SetBool("ledgeGrab", false);

        if (ledgeHitbox.upperCheck != null)
            ledgeHitbox.upperCheck.gameObject.SetActive(true);

        hasSnapped = false;
        ledgeGrabCooldownTimer = ledgeGrabCooldown;
        wallCooldownTimer = wallCooldownAfterLedge;
    }

    private void LedgeJump()
    {
        isGrabbingLedge = false;
        body.gravityScale = defaultGravityScale;
        anim.SetBool("ledgeGrab", false);

        if (ledgeHitbox.upperCheck != null)
            ledgeHitbox.upperCheck.gameObject.SetActive(true);

        hasSnapped = false;

        float pushDirection = -Mathf.Sign(transform.localScale.x);
        transform.position = new Vector2(transform.position.x + pushDirection * ledgeJumpBackDistance, transform.position.y);

        body.velocity = new Vector2(body.velocity.x, ledgeJumpPower);
        anim.SetTrigger("jump");

        ledgeGrabCooldownTimer = ledgeGrabCooldown;
        wallCooldownTimer = wallCooldownAfterLedge;
    }

    private void ReleaseLedge()
    {
        isGrabbingLedge = false;
        body.gravityScale = defaultGravityScale;
        anim.SetBool("ledgeGrab", false);

        if (ledgeHitbox.upperCheck != null)
            ledgeHitbox.upperCheck.gameObject.SetActive(true);

        hasSnapped = false;

        ledgeGrabCooldownTimer = ledgeGrabCooldown;
        wallCooldownTimer = wallCooldownAfterLedge;
    }

    // --- COLLISION CHECKS ---
    public bool isGrounded()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0,
            Vector2.down,
            0.1f,
            groundLayer
        );
        return raycastHit.collider != null;
    }

    private bool onWall()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0,
            new Vector2(transform.localScale.x, 0),
            0.1f,
            wallLayer
        );
        return raycastHit.collider != null;
    }

    public bool canAttack()
    {
        return horizontalInput == 0 && isGrounded() && !onWall();
    }
}
