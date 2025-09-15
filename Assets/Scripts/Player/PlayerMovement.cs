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

    [Header("Dash Mechanics")]
    [SerializeField] private float dashDistance;
    [SerializeField] private float dashCooldown;
    private float dashCooldownTimer;
    private bool isDashing;

    [Header("Ledge Grab")]
    [SerializeField] private LayerMask ledgeLayer;
    [SerializeField] private Vector2 ledgeCheckOffset;
    [SerializeField] private float ledgeCheckRadius = 0.1f;
    [SerializeField] private float ledgeJumpBackDistance = 0.5f; // kolik se hráè odtlaèí dozadu
    [SerializeField] private float ledgeClimbDuration = 0.5f;
    [SerializeField] private float ledgeJumpPower = 14f;

    private bool isGrabbingLedge;
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

    private float defaultGravityScale; // uložíme pùvodní gravitaci
    private float lastFacingDirection = 1f; // sleduje smìr pohledu

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        defaultGravityScale = body.gravityScale; // uložíme pùvodní hodnotu
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        // Flip player
        if (horizontalInput > 0.01f)
            transform.localScale = Vector3.one;
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        // --- Zrušení ledge grab pøi otoèení ---
        float currentFacing = Mathf.Sign(transform.localScale.x);
        if (isGrabbingLedge && currentFacing != lastFacingDirection)
        {
            ReleaseLedge();
        }
        lastFacingDirection = currentFacing;

        // Animator params
        anim.SetBool("run", horizontalInput != 0);
        anim.SetBool("grounded", isGrounded());
        anim.SetBool("onwall", onWall());

        // --- LEDGE GRAB CHECK ---
        if (onWall() && !isGrounded() && !isGrabbingLedge)
        {
            CheckForLedge();
        }

        if (isGrabbingLedge)
        {
            body.velocity = Vector2.zero;
            body.gravityScale = 0;
            anim.SetBool("ledgeGrab", true);

            // Climb nahoru
            if (Input.GetKeyDown(KeyCode.W))
                StartCoroutine(LedgeClimb());

            // Jump nahoru (upravená výška, normální animace)
            if (Input.GetKeyDown(KeyCode.Space))
                LedgeJump();

            // Pustit se dolù
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                ReleaseLedge();

            return; // blokuje ostatní pohyb
        }

        // Handle jumping
        if (Input.GetKeyDown(KeyCode.Space))
            Jump();

        // Handle dashing
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0)
            Dash();

        // Variable jump height
        if (Input.GetKeyUp(KeyCode.Space) && body.velocity.y > 0)
            body.velocity = new Vector2(body.velocity.x, body.velocity.y / 2);

        // Regular movement
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
    }

    private void Jump()
    {
        if (coyoteCounter <= 0 && !onWall() && jumpCounter <= 0) return;

        SoundManager.instance.PlaySound(jumpSound);

        if (onWall() && wallJumpCooldownTimer <= 0)
            WallJump();
        else
        {
            if (isGrounded() || coyoteCounter > 0)
            {
                body.velocity = new Vector2(body.velocity.x, jumpPower);
            }
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
    private void CheckForLedge()
    {
        Vector2 checkPos = (Vector2)transform.position + ledgeCheckOffset;
        Collider2D ledge = Physics2D.OverlapCircle(checkPos, ledgeCheckRadius, ledgeLayer);

        if (ledge != null)
        {
            isGrabbingLedge = true;
            ledgePos = ledge.transform.position;
        }
    }

    private IEnumerator LedgeClimb()
    {
        anim.SetTrigger("ledgeClimb");
        yield return new WaitForSeconds(ledgeClimbDuration);

        transform.position = new Vector2(ledgePos.x, ledgePos.y + 1f);
        isGrabbingLedge = false;
        body.gravityScale = defaultGravityScale;
        anim.SetBool("ledgeGrab", false);
    }

    private void LedgeJump()
    {
        isGrabbingLedge = false;
        body.gravityScale = defaultGravityScale;
        anim.SetBool("ledgeGrab", false);

        // Posun hráèe trochu dozadu (proti smìru zdi)
        float pushDirection = -Mathf.Sign(transform.localScale.x);
        transform.position = new Vector2(transform.position.x + pushDirection * ledgeJumpBackDistance, transform.position.y);

        // Upravený jump nahoru
        body.velocity = new Vector2(body.velocity.x, ledgeJumpPower);

        // Spustí normální jump animaci
        anim.SetTrigger("jump");
    }


    private void ReleaseLedge()
    {
        isGrabbingLedge = false;
        body.gravityScale = defaultGravityScale;
        anim.SetBool("ledgeGrab", false);
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

    // --- DEBUG GIZMOS ---
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector2 checkPos = (Vector2)transform.position + ledgeCheckOffset;
        Gizmos.DrawWireSphere(checkPos, ledgeCheckRadius);
    }
}
