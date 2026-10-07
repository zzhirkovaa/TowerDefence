using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct MoveToTowerSystem : ISystem
{
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
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, speed, stamina) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRW<Stamina>>()
                     .WithAll<CanMove>())
        {
            var position = transform.ValueRO.Position;
            var target = new float3(towerPosition.x, position.y, towerPosition.z);
            var offset = target - position;
            var distance = math.length(offset);
            if (distance <= speed.ValueRO.ReachDistance)
                continue;

            var direction = offset / distance;
            var step = math.min(speed.ValueRO.Value * deltaTime, distance - speed.ValueRO.ReachDistance);
            transform.ValueRW.Position = position + direction * step;
            transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
            stamina.ValueRW.Current = math.max(
                0f,
                stamina.ValueRO.Current - stamina.ValueRO.DrainPerSecond * deltaTime);
        }
    }
}
