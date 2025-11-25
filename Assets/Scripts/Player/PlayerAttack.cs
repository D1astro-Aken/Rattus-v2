using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackMoveDistance = 1f;
    [SerializeField] private float attackDelay = 0.3f; // Time until damage is applied
    [SerializeField] private float postAttackDelay = 0.3f;
 

    [SerializeField] private Transform attackPoint;
    [SerializeField] private int attackDamage = 40;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask Enemies;

    // 5 polí pro zvuky útoku (bude vybrán jeden náhodně)
    [SerializeField] private AudioClip attackSound1;
    [SerializeField] private AudioClip attackSound2;
    [SerializeField] private AudioClip attackSound3;
    [SerializeField] private AudioClip attackSound4;
    [SerializeField] private AudioClip attackSound5;
    [SerializeField] private AudioSource audioSource; // pokud nezadáš, script zkusí GetComponent<AudioSource>()

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

        // Pokusíme se získat AudioSource, pokud není přiřazen v inspektoru
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // @SFX:AttackInput
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && cooldownTimer > attackCooldown && !isAttacking && playerMovement.isGrounded())
        {
            StartCoroutine(PerformAttack());
        
        }

        cooldownTimer += Time.deltaTime;
    }

    // @SFX:AttackStart
    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        cooldownTimer = 0f;

        anim.SetTrigger("attack");

        // přehraj náhodný attack zvuk (pokud je dostupný)
        PlayRandomAttackSound();

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

    // @SFX:AttackHit
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

    // přehraje jeden náhodný zvuk z pěti polí (pokud existují)
    private void PlayRandomAttackSound()
    {
        if (audioSource == null) return;

        // sesbíráme dostupné clipy do pole
        AudioClip[] clips = new AudioClip[] { attackSound1, attackSound2, attackSound3, attackSound4, attackSound5 };

        // vytvoříme seznam pouze s neprázdnými clipy
        int validCount = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null) validCount++;
        }

        if (validCount == 0) return; // žádný clip přiřazen

        // vybereme náhodný index mezi těmi, které nejsou null
        int chosenIndex;
        do
        {
            chosenIndex = Random.Range(0, clips.Length);
        } while (clips[chosenIndex] == null);

        audioSource.PlayOneShot(clips[chosenIndex]);
    }

    // @SFX:DebugGizmos
    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
