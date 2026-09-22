using Unity.Burst;
using Unity.Entities;

/// <summary>
/// Враг дошёл до башни: наносит урон башне и обнуляет своё HP.
/// Дальше DeathSystem его удаляет — мобы не скапливаются на финише.
/// </summary>
[UpdateAfter(typeof(MoveAlongPathSystem))]
[UpdateBefore(typeof(DeathSystem))]
public partial struct EnemyAttackGoalSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PathWaypoint>();
        state.RequireForUpdate<PathFollower>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var waypoints = SystemAPI.GetSingletonBuffer<PathWaypoint>(true);
        var waypointCount = waypoints.Length;
        var hasTower = SystemAPI.TryGetSingletonEntity<TowerTag>(out var towerEntity);

        foreach (var (follower, damage, health) in
                 SystemAPI.Query<RefRO<PathFollower>, RefRO<DamageOnGoal>, RefRW<Health>>())
        {
            if (follower.ValueRO.WaypointIndex < waypointCount)
                continue;

            if (hasTower && SystemAPI.HasComponent<Health>(towerEntity))
            {
                var towerHealth = SystemAPI.GetComponentRW<Health>(towerEntity);
                towerHealth.ValueRW.Current -= damage.ValueRO.Value;
            }

            health.ValueRW.Current = 0f;
        }
    }
}
