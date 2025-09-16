using UnityEngine;

public class MonsterPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;

    protected int patrolDestination;
    protected Vector3 originalScale;
    protected Rigidbody2D rb;
    protected Animator anim;

    protected virtual void Start()
    {
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        Patrol();
    }

    protected void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[patrolDestination];

        // Otoèení smìrem k patrol pointu
        if (targetPoint.position.x < transform.position.x)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.2f)
        {
            patrolDestination = (patrolDestination + 1) % patrolPoints.Length;
        }
    }

    // Funkce pro okamžité nastavení patrolDestination na nejbližší bod
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
}
