using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct MoveToTowerSystem : ISystem
{
    [BurstCompile]
    [WithAll(typeof(CanMove))]
    partial struct MoveToTowerJob : IJobEntity
    {
        public float3 TowerPosition;
        public float DeltaTime;

        void Execute(ref LocalTransform transform, in MoveSpeed speed, ref Stamina stamina)
        {
            var position = transform.Position;
            var target = new float3(TowerPosition.x, position.y, TowerPosition.z);
            var offset = target - position;
            var distance = math.length(offset);
            if (distance <= speed.ReachDistance)
                return;

            var direction = offset / distance;
            var step = math.min(speed.Value * DeltaTime, distance - speed.ReachDistance);
            transform.Position = position + direction * step;
            transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
            stamina.Current = math.max(0f, stamina.Current - stamina.DrainPerSecond * DeltaTime);
        }
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TowerTarget>();
        state.RequireForUpdate<MoveSpeed>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var towerEntity = SystemAPI.GetSingletonEntity<TowerTarget>();
        var towerPosition = SystemAPI.GetComponent<LocalTransform>(towerEntity).Position;
        var job = new MoveToTowerJob
        {
            TowerPosition = towerPosition,
            DeltaTime = SystemAPI.Time.DeltaTime
        };
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}
