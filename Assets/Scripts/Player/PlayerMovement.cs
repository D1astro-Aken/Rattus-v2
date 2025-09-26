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
    [SerializeField] private float ledgeClimbDuration = 0.4f; // Rychlejší climb
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
            // Uvolnit ledge grab pokud hráč zmáčkne opačný směr
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
        Debug.Log($"Ledge Check - Grounded: {isGrounded()}, OnWall: {onWall()}, IsGrabbing: {isGrabbingLedge}, CanGrab: {ledgeHitbox.canGrab}, HasSnapped: {hasSnapped}, Cooldown: {ledgeGrabCooldownTimer}");
        
        if (!isGrounded() && !onWall() && !isGrabbingLedge && ledgeHitbox.canGrab &&
            !hasSnapped && ledgeGrabCooldownTimer <= 0)
        {
            Debug.Log("Starting ledge grab!");
            StartLedgeGrab(ledgeHitbox.ledgePosition);
        }

        if (isGrabbingLedge)
        {
            body.velocity = Vector2.zero;
            body.gravityScale = 0;
            anim.SetBool("ledgeGrab", true);

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
                    Collider2D hitCollider = Physics2D.OverlapCircle(checkPos, 0.08f, groundLayer);
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
                        
                        Collider2D testCollider = Physics2D.OverlapCircle(safePos, 0.08f, groundLayer);
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

            if (Input.GetKeyDown(KeyCode.Space))
                LedgeJump();

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                ReleaseLedge();

            if (!ledgeHitbox.canGrab)
            {
                Debug.Log("Releasing ledge because canGrab became false!");
                ReleaseLedge();
            }

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

        // Check if SoundManager exists before trying to play sound
        if (SoundManager.instance != null && jumpSound != null)
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

        // Add null check for animator
        if (anim != null)
            anim.SetTrigger("jump");
    }

    private void WallJump()
    {
        if (wallCooldownTimer > 0) return; // nelze během cooldownu

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
    }

    private IEnumerator LedgeClimb()
    {
        anim.SetTrigger("ledgeClimb");
        yield return new WaitForSeconds(ledgeClimbDuration);

        transform.position = new Vector2(ledgePos.x, ledgePos.y + 1f);
        isGrabbingLedge = false;
        body.gravityScale = defaultGravityScale;
        anim.SetBool("ledgeGrab", false);

        hasSnapped = false;
        ledgeGrabCooldownTimer = ledgeGrabCooldown;
        wallCooldownTimer = wallCooldownAfterLedge;

        ledgeHitbox.ResetLedge(); // 🔹 odemkneme pro další grab
    }

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

    // --- GIZMOS ---
    private void OnDrawGizmosSelected()
    {
        if (ledgeHitbox == null) return;

        // poslední známá pozice ledge
        Vector2 basePos = ledgeHitbox.ledgePosition;

        // směr podle toho, kam se hráč dívá
        float direction = Mathf.Sign(transform.localScale.x) * -1;

        // kde by se měl hráč snapnout
        Vector2 snapPosition = new Vector2(
            basePos.x + direction * ledgeSnapHorizontalOffset,
            basePos.y - ledgeHangOffsetY
        );

        // vykresli gizmo
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(snapPosition, 0.1f);

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawCube(snapPosition, new Vector3(0.3f, 0.3f, 0.3f));
    }
}
