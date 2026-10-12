using Unity.Entities;
using UnityEngine;

public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public int EnemyCount = 12;
    public float SpawnInterval = 0.7f;
    public float SpawnSpread = 1.2f;
}

class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        if (authoring.Prefab == null)
            return;

        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemySpawner
        {
            NextSpawnTime = 0f,
            SpawnSpread = Mathf.Max(0f, authoring.SpawnSpread)
        });

        AddComponent(entity, new EnemyPrefabStore
        {
            Enemy = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
        });

        var requests = AddBuffer<EnemySpawnRequest>(entity);
        var count = Mathf.Max(0, authoring.EnemyCount);
        var interval = Mathf.Max(0.05f, authoring.SpawnInterval);
        for (var i = 0; i < count; i++)
            requests.Add(new EnemySpawnRequest { DelayBeforeNext = interval });
    }
}
