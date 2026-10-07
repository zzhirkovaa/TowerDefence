using Unity.Entities;
using Unity.Transforms;

public partial struct EnemySpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemySpawner>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var spawner = SystemAPI.GetSingleton<EnemySpawner>();
        var enemy = state.EntityManager.Instantiate(spawner.Prefab);

        var spawnEntity = SystemAPI.GetSingletonEntity<EnemySpawner>();
        var spawnPosition = SystemAPI.GetComponent<LocalTransform>(spawnEntity).Position;
        var transform = state.EntityManager.GetComponentData<LocalTransform>(enemy);
        transform.Position = spawnPosition;
        state.EntityManager.SetComponentData(enemy, transform);
        state.Enabled = false;
    }
}
