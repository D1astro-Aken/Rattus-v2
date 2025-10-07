using UnityEngine;
using System.Collections;

public class Projectiles : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float lifeTime = 3f;
    public int damage = 1;
    
    [Header("Launch Delay Settings")]
    public float launchDelay = 0.5f; // Doba čekání před vypuštěním

    [Header("Destruction Settings")]
    [SerializeField] private LayerMask destructibleLayers = -1; // Defaultně všechny layers

    private Vector2 direction;
    private bool isLaunched = false;
    private Vector3 spawnPosition;
    private bool delayStarted = false;

    private void Start()
    {
        spawnPosition = transform.position;
        Destroy(gameObject, lifeTime + launchDelay + 1f); // Celkový životnost včetně delay + buffer
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        Debug.Log($"[Projectiles] SetDirection called with: {dir}, normalized: {direction}");
        
        // Spustí delay pouze pokud ještě nebyl spuštěn
        if (!delayStarted)
        {
            StartCoroutine(LaunchAfterDelay());
            delayStarted = true;
        }
    }
    
    public void SetLaunchDelay(float delay)
    {
        launchDelay = delay;
    }

    private IEnumerator LaunchAfterDelay()
    {
        yield return new WaitForSeconds(launchDelay);
        isLaunched = true;
        Debug.Log("Projektil byl vypuštěn po delay!");
    }

    private void Update()
    {
        if (!isLaunched)
        {
            // Projektil zůstává na spawn pozici
            transform.position = spawnPosition;
            return;
        }

        // Normální pohyb projektilu
        Debug.Log($"[Projectiles] Moving with direction: {direction}, speed: {speed}");
        transform.Translate(direction * speed * Time.deltaTime);

        // Volitelně: otočení sprite směrem pohybu
        if (direction.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else
            transform.localScale = new Vector3(1, 1, 1);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Projektil narazil do: {collision.name}, Tag: {collision.tag}, Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");

        if (collision.CompareTag("Player"))
        {
            Debug.Log("Projektil zasáhl hráče!");
            // Způsob damage hráči
            Health playerHealth = collision.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            Destroy(gameObject); // zniči projektil po zásahu
            return;
        }

        // Zkontroluj specifické layers Ground a Wall
        string layerName = LayerMask.LayerToName(collision.gameObject.layer);
        if (layerName == "Ground" || layerName == "Wall")
        {
            Debug.Log($"Projektil se ničí o {layerName} layer!");
            Destroy(gameObject);
            return;
        }

        // Zkontroluj jestli objekt je na některém z destructible layers
        int objectLayer = collision.gameObject.layer;
        
        Debug.Log($"Projektil narazil do layer: {LayerMask.LayerToName(objectLayer)} (číslo: {objectLayer})");
        Debug.Log($"Destructible layers mask: {destructibleLayers.value}");
        
        // Zkontroluj jestli layer objektu je v destructible layers
        if (((1 << objectLayer) & destructibleLayers) != 0)
        {
            Debug.Log($"Projektil se ničí o layer: {LayerMask.LayerToName(objectLayer)}!");
            Destroy(gameObject); // zniči projektil při nárazu do nastaveného layeru
        }
        else
        {
            Debug.Log($"Layer {LayerMask.LayerToName(objectLayer)} není v destructible layers.");
        }
    }
}
