using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct EnemySpawnerSystem : ISystem
{
    Random _random;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemySpawner>();
        state.RequireForUpdate<EnemyPrefabStore>();
        state.RequireForUpdate<EnemySpawnRequest>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        _random = Random.CreateFromIndex((uint)System.DateTime.UtcNow.Ticks);
    }

    public void OnUpdate(ref SystemState state)
    {
        var requests = SystemAPI.GetSingletonBuffer<EnemySpawnRequest>();
        if (requests.Length == 0)
            return;

        var spawner = SystemAPI.GetSingletonRW<EnemySpawner>();
        if (SystemAPI.Time.ElapsedTime < spawner.ValueRO.NextSpawnTime)
            return;

        var prefab = SystemAPI.GetSingleton<EnemyPrefabStore>().Enemy;
        var spawnEntity = SystemAPI.GetSingletonEntity<EnemySpawner>();
        var transform = SystemAPI.GetComponent<LocalTransform>(spawnEntity);
        transform.Position.x += _random.NextFloat(-spawner.ValueRO.SpawnSpread, spawner.ValueRO.SpawnSpread);
        transform.Position.z += _random.NextFloat(-spawner.ValueRO.SpawnSpread, spawner.ValueRO.SpawnSpread);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var enemy = ecb.Instantiate(prefab);
        ecb.SetComponent(enemy, transform);

        spawner.ValueRW.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + requests[0].DelayBeforeNext;
        requests.RemoveAt(0);
    }
}
