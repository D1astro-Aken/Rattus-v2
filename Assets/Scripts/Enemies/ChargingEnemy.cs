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

    private bool isCharging = false;
    private bool canCharge = true;
    private bool isStunned = false;

    protected override void Update()
    {
        if (!enableCharge)
        {
            base.Update(); // patrol
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
            base.Update(); // patrol
        }
    }

    private void TryCharge()
    {
        if (canCharge)
        {
            StartCoroutine(ChargeSequence());
        }
    }

    private IEnumerator ChargeSequence()
    {
        isCharging = true;
        canCharge = false;

        rb.velocity = Vector2.zero;

        Vector2 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;

        // otoèení smìrem k hráèi
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        // Wind-up
        if (anim != null) anim.SetTrigger("WindUp");
        yield return new WaitForSeconds(windUpDuration);
        if (anim != null) anim.ResetTrigger("WindUp");

        // Charge
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

        // Stun
        isStunned = true;
        if (anim != null) anim.SetTrigger("Stun");
        yield return new WaitForSeconds(stunDuration);
        if (anim != null) anim.ResetTrigger("Stun");
        isStunned = false;

        // Cooldown
        yield return new WaitForSeconds(chargeCooldown);
        canCharge = true;
        isCharging = false;
    }
}
