using System.Collections.Generic;
using UnityEngine;

public class RockField : MonoBehaviour
{
    [SerializeField] private GameObject boulderPrefab;
    [SerializeField] private FarmResources resources;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int maxBoulders = 3;
    [SerializeField] private Vector2 spawnDelay = new Vector2(15f, 25f);
    private readonly List<Boulder> boulders = new List<Boulder>();
    private float nextSpawn;

    private void Start()
    {
        for (int i = 0; i < maxBoulders; i++) Spawn();
        nextSpawn = Time.time + Random.Range(spawnDelay.x, spawnDelay.y);
    }
    private void Update()
    {
        if (Time.time < nextSpawn) return;
        boulders.RemoveAll(boulder => boulder == null);
        if (boulders.Count < maxBoulders) Spawn();
        nextSpawn = Time.time + Random.Range(spawnDelay.x, spawnDelay.y);
    }
    private void Spawn()
    {
        var empty = new List<Transform>();
        foreach (var point in spawnPoints)
            if (point.childCount == 0) empty.Add(point);
        if (empty.Count == 0) return;
        var placed = Instantiate(boulderPrefab, empty[Random.Range(0, empty.Count)], false);
        placed.name = "boulder";
        var boulder = placed.GetComponent<Boulder>();
        boulder.Initialize(resources);
        boulders.Add(boulder);
    }
}
