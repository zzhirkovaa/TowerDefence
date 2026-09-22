using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Двигает врагов по пути, только если CanMove включён (хватило стамины).
/// Пока идут — стамина тратится.
/// </summary>
public partial struct MoveAlongPathSystem : ISystem
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
        if (waypoints.Length == 0)
            return;

        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, follower, speed, stamina) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<PathFollower>, RefRO<MoveSpeed>, RefRW<Stamina>>()
                     .WithAll<CanMove>())
        {
            var index = follower.ValueRO.WaypointIndex;
            if (index >= waypoints.Length)
                continue;

            var target = waypoints[index].Position;
            var position = transform.ValueRO.Position;
            var offset = target - position;
            var distance = math.length(offset);

            if (distance <= follower.ValueRO.ReachDistance)
            {
                follower.ValueRW.WaypointIndex = index + 1;
                continue;
            }

            var direction = offset / distance;
            var stepLength = speed.ValueRO.Value * deltaTime;
            if (stepLength > distance)
                stepLength = distance;

            transform.ValueRW.Position = position + direction * stepLength;

            var look = new float3(direction.x, 0f, direction.z);
            if (math.lengthsq(look) > 0.0001f)
                transform.ValueRW.Rotation = quaternion.LookRotationSafe(look, math.up());

            stamina.ValueRW.Current = math.max(
                0f,
                stamina.ValueRO.Current - stamina.ValueRO.DrainPerSecond * deltaTime);
        }
    }
}
