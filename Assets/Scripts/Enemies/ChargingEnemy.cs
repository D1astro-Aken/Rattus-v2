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

    [Header("Sounds")]
    [SerializeField] private AudioClip[] AudioClips1;

    [Header("Alert Sound")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioClip alertClip;
    [SerializeField] private float alertVolume = 1f;
    private bool hasAlertedPlayer = false;

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
            // Alert zvuk: přehraj jednou když nepřítel poprvé spatří hráče v dosahu
            if (!hasAlertedPlayer)
            {
                hasAlertedPlayer = true;

                if (alertClip != null)
                {
                    if (ambientSource == null)
                        ambientSource = GetComponent<AudioSource>();

                    if (ambientSource != null)
                        ambientSource.PlayOneShot(alertClip, alertVolume);
                    else
                        Debug.LogWarning("[ChargingEnemy] No AudioSource available to play alert clip!");
                }
            }

            TryCharge();
        }
        else
        {
            // Mimo dosah hráče: resetuj alert flag, aby se mohl znovu přehrát
            hasAlertedPlayer = false;
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
    public IEnumerator ChargeSequence()
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

        // Zvuk po dokončení charge
        if (SoundManager.instance != null)
        {
            if (AudioClips1 != null && AudioClips1.Length > 0)
                SoundManager.instance.PlayOneOf(AudioClips1);
        }

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
}
