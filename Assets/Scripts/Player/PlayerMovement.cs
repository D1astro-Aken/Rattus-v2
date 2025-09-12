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

    private void Awake()
    {
        // Grab references for Rigidbody and Animator from the object
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        // Flip player when moving left/right
        if (horizontalInput > 0.01f)
            transform.localScale = Vector3.one;
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        // Set animator parameters
        anim.SetBool("run", horizontalInput != 0);
        anim.SetBool("grounded", isGrounded());
        anim.SetBool("onwall", onWall());

        // Handle jumping
        if (Input.GetKeyDown(KeyCode.Space))
            Jump();

        // Handle dashing
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0)
            Dash();

        // Reduce velocity for adjustable jump height
        if (Input.GetKeyUp(KeyCode.Space) && body.velocity.y > 0)
            body.velocity = new Vector2(body.velocity.x, body.velocity.y / 2);

        // Regular movement and cooldown management
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

        // Reduce wall jump cooldown timer
        if (wallJumpCooldownTimer > 0)
        {
            wallJumpCooldownTimer -= Time.deltaTime;
        }
    }


    private void Jump()
    {
        if (coyoteCounter <= 0 && !onWall() && jumpCounter <= 0) return;

        SoundManager.instance.PlaySound(jumpSound);

        if (onWall() && wallJumpCooldownTimer <= 0)  // Only allow wall jump if cooldown is over
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
    }


    private void WallJump()
    {
        // Get the direction away from the wall
        float wallDirection = -Mathf.Sign(transform.localScale.x);

        // Apply the jump force with the X and Y components
        body.velocity = new Vector2(wallDirection * wallJumpX, wallJumpY);

        // Reset the wall jump cooldown timer
        wallJumpCooldownTimer = wallJumpCooldown;

        // Optional: Add a slight delay to re-enable player input after the wall jump
        StartCoroutine(DisableInputTemporarily(0.2f));
    }


    private IEnumerator DisableInputTemporarily(float duration)
    {
        isDashing = true; // Disable movement inputs briefly
        yield return new WaitForSeconds(duration);
        isDashing = false;
    }


    private void Dash()
    {
        SoundManager.instance.PlaySound(dashSound);
        isDashing = true;
        dashCooldownTimer = dashCooldown;

        // Trigger the dash animation
        anim.SetTrigger("dash");

        Vector2 dashDirection = new Vector2(transform.localScale.x, 0).normalized;
        body.velocity = dashDirection * dashDistance;

        Invoke(nameof(EndDash), 0.2f); // Ends dash after 0.2 seconds
    }


    // test

private void EndDash()
    {
        isDashing = false;
    }

    public bool isGrounded()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        return raycastHit.collider != null;
    }

    private bool onWall()
    {
        
        RaycastHit2D raycastHit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0,
            new Vector2(transform.localScale.x, 0), // Adjust ray direction to the player's facing
            0.1f,
            wallLayer
        );
        return raycastHit.collider != null;
    }

    //cat
    public bool canAttack()
    {
        return horizontalInput == 0 && isGrounded() && !onWall();
    }
}
