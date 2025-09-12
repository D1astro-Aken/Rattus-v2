using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackMoveDistance = 1f;
    [SerializeField] private float attackDelay = 0.3f; // Time until damage is applied
    [SerializeField] private float postAttackDelay = 0.3f;
    [SerializeField] private AudioClip SwordSwingSFX;

    [SerializeField] private Transform attackPoint;
    [SerializeField] private int attackDamage = 40;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask Enemies;

    private Animator anim;
    private PlayerMovement playerMovement;
    private Rigidbody2D rb;

    private float cooldownTimer = Mathf.Infinity;
    private bool isAttacking = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && cooldownTimer > attackCooldown && !isAttacking && playerMovement.isGrounded())
        {
            StartCoroutine(PerformAttack());
            SoundManager.instance.PlaySound(SwordSwingSFX);
        }

        cooldownTimer += Time.deltaTime;
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        cooldownTimer = 0f;

        anim.SetTrigger("attack");

        // Move player slightly in facing direction
        Vector2 moveDir = new Vector2(transform.localScale.x, 0).normalized;
        rb.velocity = new Vector2(moveDir.x * attackMoveDistance, rb.velocity.y);

        // Wait for the right moment to apply damage
        yield return new WaitForSeconds(attackDelay);
        ApplyDamage();

        // Wait before player can act again
        yield return new WaitForSeconds(postAttackDelay);
        rb.velocity = new Vector2(0, rb.velocity.y); // stop horizontal motion

        isAttacking = false;
    }

    private void ApplyDamage()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, Enemies);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                Vector2 knockbackDir = (enemy.transform.position - attackPoint.position).normalized;
                enemy.TakeDamage(attackDamage, knockbackDir);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
