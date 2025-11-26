using UnityEngine;

public class ProximityMine : MonoBehaviour
{
    public float lockOnRange = 6f;
    public float followSpeed = 2f;
    public float explodeRadius = 1.25f;
    public float damage = 20f;
    public float lifeTime = 12f;

    private Transform target;
    private MineBee owner;
    private float spawnTime;

    private void Awake()
    {
        spawnTime = Time.time;
    }

    public void Initialize(Transform player, MineBee bee)
    {
        target = player;
        owner = bee;
    }

    private void Update()
    {
        if (Time.time - spawnTime >= lifeTime)
        {
            Explode();
            return;
        }

        if (target == null) return;

        float d = Vector2.Distance(transform.position, target.position);
        if (d <= lockOnRange)
        {
            Vector2 dir = (target.position - transform.position).normalized;
            transform.position += (Vector3)(dir * followSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Explode();
            return;
        }

        string ln = LayerMask.LayerToName(other.gameObject.layer);
        if (ln == "Ground" || ln == "Wall")
        {
            Explode();
        }
    }

    private void Explode()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explodeRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag("Player"))
            {
                var h = hits[i].GetComponent<Health>();
                if (h != null) h.TakeDamage(damage);
            }
        }

        if (owner != null) owner.NotifyMineDestroyed(this);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }
}
