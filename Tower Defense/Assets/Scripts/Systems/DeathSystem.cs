using Unity.Entities;
using UnityEngine;

/// <summary>
/// HP меньше или равно 0: враг уничтожается, башня получает GameOver.
/// </summary>
[UpdateAfter(typeof(EnemyAttackGoalSystem))]
public partial struct DeathSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Health>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (health, entity) in SystemAPI.Query<RefRO<Health>>().WithEntityAccess())
        {
            if (health.ValueRO.Current > 0f)
                continue;

            if (SystemAPI.HasComponent<TowerTag>(entity))
            {
                if (!SystemAPI.HasComponent<GameOver>(entity))
                {
                    ecb.AddComponent<GameOver>(entity);
                    Debug.Log("Башня разрушена. Game Over.");
                }

                continue;
            }

            ecb.DestroyEntity(entity);
        }
    }
}
