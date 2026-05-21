using UnityEngine;

public class Urchin : MonsterPatrol
{
    public enum UrchinState { Stationary, Patrol, Rolling }

    public Transform playerTransform;
    public float nearDistance = 3f;
    public float nearTimeToHarden = 1.5f;
    public float stationaryDuration = 3f;
    public float rollingSpeed = 6f;
    public float rollingDuration = 4f;
    public float rollEndDistance = 8f;

    [Header("Sounds")]
    // Sounds for contact and hits
    public AudioClip[] contactSounds;
    public AudioClip[] hitSounds;
    private AudioSource audioSource;

    private UrchinState state = UrchinState.Patrol;
    private Enemy enemy;
    private float nearTimer;
    private float stateStartTime;
    private int rollDirection = 1;
    private bool stuckInRoll = false;
    public bool IsRollingState => state == UrchinState.Rolling;
    public bool IsPatrolState => state == UrchinState.Patrol;

    protected override void Start()
    {
        base.Start();
        enemy = GetComponent<Enemy>();
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // Enable 3D spatial sound (1.0 = 3D, 0.0 = 2D)
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 15f;

        EnterPatrol();
    }

    protected override void Update()
    {
        switch (state)
        {
            case UrchinState.Stationary:
                UpdateStationary();
                break;
            case UrchinState.Patrol:
                UpdatePatrol();
                break;
            case UrchinState.Rolling:
                UpdateRolling();
                break;
        }
    }

    private void UpdateStationary()
    {
        if (Time.time - stateStartTime >= stationaryDuration)
        {
            EnterPatrol();
            return;
        }

        if (anim != null)
        {
            SetAnimFlags(isIdle: true, isWalking: false, isRolling: false);
        }
    }

    private void UpdatePatrol()
    {
        base.Update();

        if (playerTransform != null)
        {
            float d = Vector2.Distance(transform.position, playerTransform.position);
            if (d <= nearDistance)
            {
                nearTimer += Time.deltaTime;
                if (nearTimer >= nearTimeToHarden)
                {
                    EnterStationary();
                    return;
                }
            }
            else
            {
                nearTimer = 0f;
            }
        }

        if (anim != null)
        {
            SetAnimFlags(isIdle: false, isWalking: true, isRolling: false);
        }
    }

    private void UpdateRolling()
    {
        if (stuckInRoll && rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        bool timeDone = Time.time - stateStartTime >= rollingDuration;
        bool farAway = playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) >= rollEndDistance;
        if (timeDone || farAway)
        {
            EnterPatrol();
            return;
        }

        if (anim != null)
        {
            SetAnimFlags(isIdle: false, isWalking: false, isRolling: true);
        }
    }

    private void EnterStationary()
    {
        state = UrchinState.Stationary;
        stateStartTime = Time.time;
        nearTimer = 0f;
        stuckInRoll = false;
        if (enemy != null)
        {
            enemy.damageable = false;
            enemy.knockbackOnly = false;
        }
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;
        }
        transform.rotation = Quaternion.identity;
    }
    private void EnterPatrol()
    {
        state = UrchinState.Patrol;
        stateStartTime = Time.time;
        nearTimer = 0f;
        stuckInRoll = false;
        if (enemy != null)
        {
            enemy.damageable = true;
            enemy.knockbackOnly = false;
            enemy.knockbackMultiplier = 3f;
        }
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        transform.rotation = Quaternion.identity;
    }

    private void EnterRolling(Vector2 fromAttackDir)
    {
        state = UrchinState.Rolling;
        stateStartTime = Time.time;
        nearTimer = 0f;
        stuckInRoll = false;
        if (enemy != null)
        {
            enemy.damageable = false;
            enemy.knockbackOnly = true;
            enemy.knockbackMultiplier = 6f;
        }

        if (fromAttackDir.x == 0)
            rollDirection = transform.localScale.x >= 0 ? 1 : -1;
        else
            rollDirection = fromAttackDir.x > 0 ? 1 : -1;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.None;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("[Urchin] Collision with Player detected (OnCollisionEnter2D)");
            PlayRandomContactSound();
            return;
        }

        if (state != UrchinState.Rolling) return;
        var other = collision.collider;
        string ln = LayerMask.LayerToName(other.gameObject.layer);
        if (ln == "Ground" || ln == "Wall")
        {
            stuckInRoll = true;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Urchin] Trigger with Player detected (OnTriggerEnter2D)");
            PlayRandomContactSound();
            return;
        }

        if (state != UrchinState.Rolling) return;
        string ln = LayerMask.LayerToName(other.gameObject.layer);
        if (ln == "Ground" || ln == "Wall")
        {
            stuckInRoll = true;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }

    public void PrepareForKnockbackDuringRoll()
    {
        if (state != UrchinState.Rolling) return;
        stuckInRoll = false;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.None;
        }
    }

    public void OnDamagedDuringPatrol(Vector2 knockbackDirection)
    {
        if (state != UrchinState.Patrol) return;
        EnterRolling(knockbackDirection);
    }

    private void SetAnimFlags(bool isIdle, bool isWalking, bool isRolling)
    {
        if (anim == null) return;
        if (HasParam("isIdle")) anim.SetBool("isIdle", isIdle);
        if (HasParam("isWalking")) anim.SetBool("isWalking", isWalking);
        if (HasParam("isRolling")) anim.SetBool("isRolling", isRolling);
    }

    private bool HasParam(string name)
    {
        foreach (var p in anim.parameters)
        {
            if (p.name == name) return true;
        }
        return false;
    }

    private void PlayRandomContactSound()
    {
        if (contactSounds != null && contactSounds.Length > 0 && audioSource != null)
        {
            // Pick a random sound from the array
            int index = Random.Range(0, contactSounds.Length);
            // Use PlayOneShot so it doesn't cut off if triggered rapidly, 
            // though for damage sounds usually one at a time is fine.
            audioSource.PlayOneShot(contactSounds[index]);
        }
    }

    public void PlayHitSound()
    {
        if (hitSounds != null && hitSounds.Length > 0 && audioSource != null)
        {
            int index = Random.Range(0, hitSounds.Length);
            audioSource.PlayOneShot(hitSounds[index]);
            Debug.Log($"[Urchin] Playing hit sound: {hitSounds[index].name}");
        }
        else
        {
             Debug.LogWarning("[Urchin] Cannot play hit sound. Check if hitSounds are assigned and AudioSource exists.");
        }
    }
}
