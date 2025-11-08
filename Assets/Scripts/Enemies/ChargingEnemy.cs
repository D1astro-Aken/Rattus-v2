using System.Collections;
using UnityEngine;

public class ChargingEnemy : MonsterPatrol
{
    [Header("Charge Settings")]
    public Transform playerTransform;
    public bool enableCharge = true;
    public float triggerDistance = 4f;
    public float windUpDuration = 0.5f;
    public float chargeSpeed = 8f;
    public float chargeDuration = 1f;
    public float chargeCooldown = 3f;
    public float stunDuration = 1f;

    [Header("Charge Impact Sounds")]
    public AudioClip playerImpactSFX;
    public AudioClip wallImpactSFX;
    public AudioSource audioSource;

    private bool isCharging = false;
    private bool canCharge = true;
    private bool isStunned = false;

    // @SFX:EnemyInit
    protected override void Start()
    {
        base.Start();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    // @SFX:EnemyUpdate
    protected override void Update()
    {
        if (!enableCharge)
        {
            base.Update(); // jen patrol
            return;
        }

        if (isCharging || isStunned)
            return;

        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < triggerDistance)
        {
            TryCharge();
        }
        else
        {
            base.Update(); // jinak patrol
        }
    }

    // @SFX:ChargeTrigger
    private void TryCharge()
    {
        if (canCharge)
            StartCoroutine(ChargeSequence());
    }

    // @SFX:ChargeSequence
    private IEnumerator ChargeSequence()
    {
        isCharging = true;
        canCharge = false;

        rb.velocity = Vector2.zero;

        Vector2 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;

        // otočení směrem k hráči
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        // Wind-up animace
        if (anim != null) anim.SetTrigger("WindUp");
        yield return new WaitForSeconds(windUpDuration);
        if (anim != null) anim.ResetTrigger("WindUp");

        // Charge animace
        if (anim != null) anim.SetTrigger("Charge");
        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            rb.velocity = new Vector2(direction.x * chargeSpeed, rb.velocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.velocity = Vector2.zero;
        if (anim != null) anim.ResetTrigger("Charge");

        // Stun animace
        isStunned = true;
        if (anim != null) anim.SetTrigger("Stun");
        yield return new WaitForSeconds(stunDuration);
        if (anim != null) anim.ResetTrigger("Stun");
        isStunned = false;

        // Vrátíme se k nejbližšímu patrol pointu
        ReturnToNearestPatrolPoint();

        // Cooldown
        yield return new WaitForSeconds(chargeCooldown);
        canCharge = true;
        isCharging = false;
    }

    // @SFX:ChargeImpact
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isCharging) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            audioSource?.PlayOneShot(playerImpactSFX);
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            audioSource?.PlayOneShot(wallImpactSFX);
        }
    }
}
