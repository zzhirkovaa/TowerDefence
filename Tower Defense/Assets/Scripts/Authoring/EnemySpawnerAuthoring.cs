using Unity.Entities;
using UnityEngine;

/// <summary>
/// Спавнер врагов. Ставится в Sub Scene, в Prefab — префаб с EnemyAuthoring.
/// </summary>
public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float SpawnInterval = 0.6f;
    public int MaxAlive = 40;
}

class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        if (authoring.Prefab == null)
            return;

        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new EnemySpawner
        {
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
            SpawnInterval = authoring.SpawnInterval,
            NextSpawnTime = 0f,
            MaxAlive = authoring.MaxAlive
        });
    }
}
