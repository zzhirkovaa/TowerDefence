using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(MoveToTowerSystem))]
public partial struct RemoveAtTowerSystem : ISystem
{
    [BurstCompile]
    partial struct RemoveAtTowerJob : IJobEntity
    {
        public float2 TowerPosition;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        void Execute(Entity entity, [ChunkIndexInQuery] int sortKey,
            in LocalTransform transform, in MoveSpeed speed)
        {
            var offset = TowerPosition - transform.Position.xz;
            var reach = speed.ReachDistance;
            if (math.lengthsq(offset) <= reach * reach)
                CommandBuffer.DestroyEntity(sortKey, entity);
        }
    }

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
        var job = new RemoveAtTowerJob
        {
            TowerPosition = towerPosition.xz,
            CommandBuffer = ecb.AsParallelWriter()
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}
