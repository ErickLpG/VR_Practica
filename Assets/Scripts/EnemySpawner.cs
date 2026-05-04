using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    #region Referencias

    [Header("Enemy")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    #endregion

    #region Configuracion

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxEnemies = 10;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool randomSpawnPoint = true;

    #endregion

    #region Variables internas

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine spawnCoroutine;

    #endregion

    #region Unity

    private void OnEnable()
    {
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    #endregion

    #region Spawn

    private IEnumerator SpawnLoop()
    {
        if (spawnOnStart)
        {
            TrySpawnEnemy();
        }

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            TrySpawnEnemy();
        }
    }

    private void TrySpawnEnemy()
    {
        CleanNullEnemies();

        if (enemyPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] No hay enemyPrefab asignado.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] No hay puntos de spawn asignados.");
            return;
        }

        if (spawnedEnemies.Count >= maxEnemies)
        {
            return;
        }

        Transform spawnPoint = GetSpawnPoint();

        GameObject enemy = Instantiate(
            enemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        spawnedEnemies.Add(enemy);
    }

    private Transform GetSpawnPoint()
    {
        if (randomSpawnPoint)
        {
            int index = Random.Range(0, spawnPoints.Length);
            return spawnPoints[index];
        }

        int nextIndex = spawnedEnemies.Count % spawnPoints.Length;
        return spawnPoints[nextIndex];
    }

    private void CleanNullEnemies()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);
            }
        }
    }

    #endregion

    #region Metodos publicos

    public void StartSpawner()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnLoop());
        }
    }

    public void StopSpawner()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    public void DestroyAllEnemies()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] != null)
            {
                Destroy(spawnedEnemies[i]);
            }
        }

        spawnedEnemies.Clear();
    }

    #endregion
}