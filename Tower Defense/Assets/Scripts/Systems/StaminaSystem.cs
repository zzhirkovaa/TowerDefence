using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[UpdateBefore(typeof(MoveToTowerSystem))]
public partial struct StaminaSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Stamina>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (stamina, entity) in SystemAPI.Query<RefRW<Stamina>>().WithEntityAccess())
        {
            var canMove = SystemAPI.IsComponentEnabled<CanMove>(entity);

            if (canMove && stamina.ValueRO.Current <= 0f)
            {
                SystemAPI.SetComponentEnabled<CanMove>(entity, false);
                canMove = false;
            }

            if (!canMove)
            {
                stamina.ValueRW.Current = math.min(
                    stamina.ValueRO.Max,
                    stamina.ValueRO.Current + stamina.ValueRO.RegenPerSecond * deltaTime);

                if (stamina.ValueRO.Current >= stamina.ValueRO.MinToMove)
                    SystemAPI.SetComponentEnabled<CanMove>(entity, true);
            }
        }
    }
}
