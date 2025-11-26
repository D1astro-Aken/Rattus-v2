using System.Collections.Generic;
using UnityEngine;

public class MineBee : MonsterPatrol
{
    public Transform playerTransform;
    public GameObject minePrefab;
    public Transform spawnPoint;
    public float detectionRange = 6f;
    public float spawnCooldown = 2f;
    public int maxActiveMines = 3;

    private float nextSpawnTime;
    private readonly List<ProximityMine> activeMines = new List<ProximityMine>();

    protected override void Start()
    {
        base.Start();
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (playerTransform == null || minePrefab == null) return;

        for (int i = activeMines.Count - 1; i >= 0; i--)
        {
            if (activeMines[i] == null) activeMines.RemoveAt(i);
        }

        if (Time.time >= nextSpawnTime && activeMines.Count < maxActiveMines)
        {
            if (Vector2.Distance(transform.position, playerTransform.position) <= detectionRange)
            {
                SpawnMine();
                nextSpawnTime = Time.time + spawnCooldown;
            }
        }
    }

    private void SpawnMine()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        var go = Object.Instantiate(minePrefab, pos, Quaternion.identity);
        var mine = go.GetComponent<ProximityMine>();
        if (mine != null)
        {
            mine.Initialize(playerTransform, this);
            activeMines.Add(mine);
        }
    }

    public void NotifyMineDestroyed(ProximityMine mine)
    {
        if (mine == null) return;
        int idx = activeMines.IndexOf(mine);
        if (idx >= 0) activeMines.RemoveAt(idx);
    }
}
