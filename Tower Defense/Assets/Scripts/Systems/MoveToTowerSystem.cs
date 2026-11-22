using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct MoveToTowerSystem : ISystem
{
    [BurstCompile]
    [WithAll(typeof(CanMove))]
    partial struct MoveToTowerJob : IJobEntity
    {
        public float3 TowerPosition;
        public float DeltaTime;
        [ReadOnly] public CollisionWorld CollisionWorld;

        void Execute(ref LocalTransform transform, ref PhysicsVelocity velocity,
            in MoveSpeed speed, ref Stamina stamina)
        {
            var position = transform.Position;
            var target = new float3(TowerPosition.x, position.y, TowerPosition.z);
            var ray = new RaycastInput
            {
                Start = position,
                End = target,
                Filter = new CollisionFilter
                {
                    BelongsTo = 1u << 1,
                    CollidesWith = 1u << 0,
                    GroupIndex = 0
                }
            };

            if (CollisionWorld.CastRay(ray, out var hit))
            {
                var towardTower = math.normalizesafe(target - position);
                target = hit.Position + towardTower * 0.35f;
            }
            var offset = target - position;
            var distance = math.length(offset);
            if (distance <= speed.ReachDistance)
            {
                velocity.Linear = float3.zero;
                return;
            }

            var direction = offset / distance;
            var moveSpeed = math.min(speed.Value, (distance - speed.ReachDistance) / DeltaTime);
            velocity.Linear = direction * moveSpeed;
            velocity.Angular = float3.zero;
            transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
            stamina.Current = math.max(0f, stamina.Current - stamina.DrainPerSecond * DeltaTime);
        }
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TowerTarget>();
        state.RequireForUpdate<MoveSpeed>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var towerEntity = SystemAPI.GetSingletonEntity<TowerTarget>();
        var towerPosition = SystemAPI.GetComponent<LocalTransform>(towerEntity).Position;
        var job = new MoveToTowerJob
        {
            TowerPosition = towerPosition,
            DeltaTime = SystemAPI.Time.DeltaTime,
            CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}
