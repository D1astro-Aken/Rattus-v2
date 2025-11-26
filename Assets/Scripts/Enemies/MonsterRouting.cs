using UnityEngine;

public class MonsterPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    public bool stopPatrolWhenAirborne = false;
    public float stuckTimeout = 1.5f;
    public float minProgressThreshold = 0.02f;
    public bool enableAutoRoam = true;
    public float autoRoamWallCheckDistance = 0.3f;
    public float autoRoamGroundCheckDistance = 0.3f;
    public bool enableResetOnStuck = true;
    public float autoRoamMaxDuration = 4f;
    public float airborneTimeout = 0.3f;
    public float autoRoamWallCheckBoxHeightFactor = 0.9f;
    public float autoRoamWallCheckBoxWidth = 0.12f;

    protected int patrolDestination;
    protected Vector3 originalScale;
    protected Rigidbody2D rb;
    protected Animator anim;
    private Collider2D col;
    private float lastDistanceToTarget;
    private float noProgressTimer;
    private bool inAutoRoam;
    private int autoRoamDirection = 1;
    private float autoRoamStartTime;
    private Vector3 spawnPosition;
    private float airborneTimer;

    protected virtual void Start()
    {
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        autoRoamDirection = transform.localScale.x >= 0 ? 1 : -1;
        spawnPosition = transform.position;
    }

    protected virtual void Update()
    {
        Patrol();
    }

    protected void Patrol()
    {
        if (inAutoRoam)
        {
            AutoRoam();
            return;
        }
        if (patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[patrolDestination];

        // Oto�en� sm�rem k patrol pointu
        if (targetPoint.position.x < transform.position.x)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        if (!stopPatrolWhenAirborne || IsGrounded())
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);
        }

        float dist = Vector2.Distance(transform.position, targetPoint.position);
        bool grounded = IsGrounded();
        if (grounded)
        {
            if (dist < lastDistanceToTarget - minProgressThreshold)
                noProgressTimer = 0f;
            else
                noProgressTimer += Time.deltaTime;
            airborneTimer = 0f;
        }
        else
        {
            noProgressTimer += Time.deltaTime;
            airborneTimer += Time.deltaTime;
            if (enableAutoRoam && airborneTimer >= airborneTimeout)
            {
                EnterAutoRoam();
                airborneTimer = 0f;
                return;
            }
        }
        lastDistanceToTarget = dist;

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.2f)
        {
            patrolDestination = (patrolDestination + 1) % patrolPoints.Length;
            noProgressTimer = 0f;
        }
        else if (noProgressTimer >= stuckTimeout)
        {
            if (enableAutoRoam)
            {
                EnterAutoRoam();
            }
            else
            {
                if (enableResetOnStuck)
                    ResetToSpawn();
                else
                    ReturnToNearestPatrolPoint();
            }
            noProgressTimer = 0f;
        }
    }

    // Funkce pro okam�it� nastaven� patrolDestination na nejbli��� bod
    public void ReturnToNearestPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        float minDistance = float.MaxValue;
        int nearestIndex = 0;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float distance = Vector2.Distance(transform.position, patrolPoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }
        patrolDestination = nearestIndex;
    }

    private bool IsGrounded()
    {
        if (col == null) return true;
        Vector2 feetCenter = new Vector2(col.bounds.center.x, col.bounds.min.y + 0.02f);
        Vector2 size = new Vector2(col.bounds.size.x * 0.9f, 0.1f);
        int mask = LayerMask.GetMask("Ground", "Wall");
        RaycastHit2D hit = Physics2D.BoxCast(feetCenter, size, 0f, Vector2.down, 0.05f, mask);
        return hit.collider != null && hit.normal.y > 0.5f;
    }

    private void EnterAutoRoam()
    {
        inAutoRoam = true;
        autoRoamDirection = transform.localScale.x >= 0 ? 1 : -1;
        autoRoamStartTime = Time.time;
    }

    private void ExitAutoRoam()
    {
        inAutoRoam = false;
    }

    private void AutoRoam()
    {
        if (stopPatrolWhenAirborne && !IsGrounded()) return;

        if (enableResetOnStuck && (Time.time - autoRoamStartTime) >= autoRoamMaxDuration)
        {
            ResetToSpawn();
            return;
        }

        if (WallAhead() || EdgeAhead())
        {
            autoRoamDirection = -autoRoamDirection;
            float sx = Mathf.Abs(originalScale.x) * autoRoamDirection;
            transform.localScale = new Vector3(sx, originalScale.y, originalScale.z);
        }

        Vector3 step = new Vector3(autoRoamDirection * moveSpeed * Time.deltaTime, 0f, 0f);
        transform.position += step;

        if (patrolPoints.Length > 0)
        {
            Transform targetPoint = patrolPoints[patrolDestination];
            float dist = Vector2.Distance(transform.position, targetPoint.position);
            if (dist < 0.5f)
            {
                ExitAutoRoam();
            }
        }
    }

    private bool WallAhead()
    {
        if (col == null) return false;
        float dir = autoRoamDirection;
        float halfWidth = col.bounds.size.x * 0.5f;
        Vector2 boxCenter = new Vector2(
            col.bounds.center.x + dir * (halfWidth + autoRoamWallCheckDistance * 0.5f),
            col.bounds.center.y
        );
        Vector2 boxSize = new Vector2(
            autoRoamWallCheckBoxWidth,
            col.bounds.size.y * autoRoamWallCheckBoxHeightFactor
        );
        int mask = LayerMask.GetMask("Wall", "Ground");
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCenter,
            boxSize,
            0f,
            Vector2.right * dir,
            autoRoamWallCheckDistance,
            mask
        );
        return hit.collider != null;
    }

    private bool EdgeAhead()
    {
        if (col == null) return false;
        float dir = autoRoamDirection;
        Vector2 frontFoot = new Vector2(col.bounds.center.x + dir * (col.bounds.size.x * 0.5f + 0.01f), col.bounds.min.y);
        int mask = LayerMask.GetMask("Ground", "Wall");
        RaycastHit2D hit = Physics2D.Raycast(frontFoot, Vector2.down, autoRoamGroundCheckDistance, mask);
        return hit.collider == null;
    }

    private void ResetToSpawn()
    {
        if (anim != null && HasParam("reset")) anim.SetTrigger("reset");
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        transform.position = spawnPosition;
        ExitAutoRoam();
        ReturnToNearestPatrolPoint();
    }

    private bool HasParam(string name)
    {
        if (anim == null) return false;
        for (int i = 0; i < anim.parameters.Length; i++)
        {
            if (anim.parameters[i].name == name) return true;
        }
        return false;
    }
}
