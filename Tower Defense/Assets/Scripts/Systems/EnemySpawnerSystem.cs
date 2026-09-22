using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Создаёт entity врагов из префаба и ставит их на первую точку пути.
/// Небольшой разброс позиции и скорости — элемент случайности из требований.
/// </summary>
public partial struct EnemySpawnerSystem : ISystem
{
    EntityQuery _aliveEnemies;
    Random _random;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemySpawner>();
        state.RequireForUpdate<PathWaypoint>();
        _aliveEnemies = SystemAPI.QueryBuilder().WithAll<PathFollower, MoveSpeed>().Build();
        _random = Random.CreateFromIndex((uint)System.DateTime.Now.Ticks);
    }

    public void OnUpdate(ref SystemState state)
    {
        var spawner = SystemAPI.GetSingletonRW<EnemySpawner>();
        var waypoints = SystemAPI.GetSingletonBuffer<PathWaypoint>(true);

        if (SystemAPI.HasSingleton<GameOver>())
            return;

        if (waypoints.Length == 0)
            return;

        if (_aliveEnemies.CalculateEntityCount() >= spawner.ValueRO.MaxAlive)
            return;

        if (SystemAPI.Time.ElapsedTime < spawner.ValueRO.NextSpawnTime)
            return;

        var enemy = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);

        var start = waypoints[0].Position;
        var sideOffset = _random.NextFloat(-1.2f, 1.2f);
        var spawnPosition = start + new float3(sideOffset, 0f, 0f);

        var transform = state.EntityManager.GetComponentData<LocalTransform>(enemy);
        transform.Position = spawnPosition;
        state.EntityManager.SetComponentData(enemy, transform);

        var speed = state.EntityManager.GetComponentData<MoveSpeed>(enemy);
        speed.Value *= _random.NextFloat(0.8f, 1.2f);
        state.EntityManager.SetComponentData(enemy, speed);

        var health = state.EntityManager.GetComponentData<Health>(enemy);
        health.Current *= _random.NextFloat(0.85f, 1.15f);
        health.Max = health.Current;
        state.EntityManager.SetComponentData(enemy, health);

        spawner.ValueRW.NextSpawnTime =
            (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnInterval;
    }
}
