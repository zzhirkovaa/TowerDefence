using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(MoveToTowerSystem))]
public partial struct RemoveAtTowerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TowerTarget>();
        state.RequireForUpdate<MoveSpeed>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var tower = SystemAPI.GetSingletonEntity<TowerTarget>();
        var towerPosition = SystemAPI.GetComponent<LocalTransform>(tower).Position;
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (transform, speed, entity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<MoveSpeed>>().WithEntityAccess())
        {
            var offset = towerPosition.xz - transform.ValueRO.Position.xz;
            var reach = speed.ValueRO.ReachDistance;
            if (math.lengthsq(offset) <= reach * reach)
                ecb.DestroyEntity(entity);
        }
    }
}
