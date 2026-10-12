using Unity.Entities;

public struct EnemySpawner : IComponentData
{
    public float NextSpawnTime;
    public float SpawnSpread;
}
