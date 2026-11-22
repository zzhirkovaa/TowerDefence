using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(MoveToTowerSystem))]
public partial struct StaminaSystem : ISystem
{
    [BurstCompile]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    partial struct StaminaJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(ref Stamina stamina, ref PhysicsVelocity velocity,
            EnabledRefRW<CanMove> canMove)
        {
            if (canMove.ValueRO && stamina.Current <= 0f)
                canMove.ValueRW = false;

            if (!canMove.ValueRO)
            {
                velocity.Linear = float3.zero;
                stamina.Current = math.min(
                    stamina.Max,
                    stamina.Current + stamina.RegenPerSecond * DeltaTime);

                if (stamina.Current >= stamina.MinToMove)
                    canMove.ValueRW = true;
            }
        }
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Stamina>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new StaminaJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}
