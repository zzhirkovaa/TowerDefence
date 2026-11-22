using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Mathematics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct RemoveAtTowerSystem : ISystem
{
    [BurstCompile]
    struct RemoveAtTowerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<TowerTarget> Towers;
        [ReadOnly] public ComponentLookup<MoveSpeed> Enemies;
        public ComponentLookup<TowerHealth> Health;
        public EntityCommandBuffer CommandBuffer;

        public void Execute(TriggerEvent triggerEvent)
        {
            var a = triggerEvent.EntityA;
            var b = triggerEvent.EntityB;
            if (Towers.HasComponent(a) && Enemies.HasComponent(b))
                DamageTower(a, b);
            else if (Towers.HasComponent(b) && Enemies.HasComponent(a))
                DamageTower(b, a);
        }

        void DamageTower(Entity tower, Entity enemy)
        {
            var health = Health[tower];
            health.Current = math.max(0, health.Current - 10);
            Health[tower] = health;
            CommandBuffer.DestroyEntity(enemy);
        }
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TowerTarget>();
        state.RequireForUpdate<TowerHealth>();
        state.RequireForUpdate<MoveSpeed>();
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var job = new RemoveAtTowerJob
        {
            Towers = SystemAPI.GetComponentLookup<TowerTarget>(true),
            Enemies = SystemAPI.GetComponentLookup<MoveSpeed>(true),
            Health = SystemAPI.GetComponentLookup<TowerHealth>(),
            CommandBuffer = ecb
        };
        state.Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
}
