using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackMoveDistance = 1f;
    [SerializeField] private float attackDelay = 0.3f; // Time until damage is applied
    [SerializeField] private float postAttackDelay = 0.3f;

    [Header("Heavy Attack")]
    [SerializeField] private float heavyAttackCooldown = 1.5f;
    [SerializeField] private int heavyAttackDamage = 70;
    [SerializeField] private float heavyKnockbackMultiplier = 2.0f;
    [SerializeField] private float heavyAttackDelay = 0.5f; // Time until damage is applied for heavy attack
    [SerializeField] private float chargeTime = 1.0f; // Time to hold button to trigger heavy attack

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
    private float chargeTimer = 0f;
    private bool isCharging = false;

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
        cooldownTimer += Time.deltaTime;

        if (!isAttacking && playerMovement.isGrounded())
        {
            // Charge-up Logic (Hold Left Click)
            if (Input.GetMouseButton(0))
            {
                if (!isCharging)
                {
                    isCharging = true;
                    chargeTimer = 0f;
                    anim.SetBool("isCharging", true);
                }

                chargeTimer += Time.deltaTime;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (isCharging)
                {
                    isCharging = false;
                    anim.SetBool("isCharging", false);

                    if (chargeTimer >= chargeTime && cooldownTimer > heavyAttackCooldown)
                    {
                        // Charged enough -> Heavy Attack
                        StartCoroutine(PerformAttack(heavyAttackDamage, heavyKnockbackMultiplier, "heavyAttack", heavyAttackCooldown, heavyAttackDelay));
                    }
                    else if (cooldownTimer > attackCooldown)
                    {
                        // Released too early -> Normal Attack
                        StartCoroutine(PerformAttack(attackDamage, 1f, "attack", attackCooldown, attackDelay));
                    }
                }
            }
        }
    }

    // @SFX:AttackStart
    private IEnumerator PerformAttack(int damage, float knockbackMult, string triggerName, float cooldown, float delay)
    {
        isAttacking = true;
        cooldownTimer = 0f; // Reset timer, although we use the passed cooldown for checking, this resets the counter

        anim.SetTrigger(triggerName);

        // přehraj náhodný attack zvuk (pokud je dostupný)
        PlayRandomAttackSound();

        // Move player slightly in facing direction
        Vector2 moveDir = new Vector2(transform.localScale.x, 0).normalized;
        rb.velocity = new Vector2(moveDir.x * attackMoveDistance, rb.velocity.y);

        // Wait for the right moment to apply damage
        yield return new WaitForSeconds(delay);
        ApplyDamage(damage, knockbackMult);

        // Wait before player can act again
        yield return new WaitForSeconds(postAttackDelay);
        rb.velocity = new Vector2(0, rb.velocity.y); // stop horizontal motion

        isAttacking = false;
        
        // Ensure cooldown timer is respected relative to when attack started or ended? 
        // Existing logic used a simple timer accumulation. 
        // If we want different cooldowns, we just need to ensure the check in Update respects it.
        // Since we reset cooldownTimer to 0 at start, and check it against limit in Update, 
        // passing 'cooldown' here isn't strictly needed for the timer logic itself unless we want to dynamically set the limit variable.
        // But the check is done BEFORE calling this.
        // So effectively, the cooldown is enforced by the time passing until the next click.
    }

    // @SFX:AttackHit
    private void ApplyDamage(int damage, float knockbackMult)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, Enemies);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                Vector2 finalKnockback = (enemy.transform.position - attackPoint.position).normalized * knockbackMult;
                enemy.TakeDamage(damage, finalKnockback);
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
