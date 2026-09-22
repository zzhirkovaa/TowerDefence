using Unity.Entities;

/// <summary>
/// Данные спавнера волн. Сам спавнер — одна entity, врагов она создаёт из префаба.
/// </summary>
public struct EnemySpawner : IComponentData
{
    public Entity Prefab;
    public float SpawnInterval;
    public float NextSpawnTime;
    public int MaxAlive;
}
