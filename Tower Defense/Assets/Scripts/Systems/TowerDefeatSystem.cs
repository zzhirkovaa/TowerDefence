using System;
using Unity.Entities;

public static class TowerDefeatEvent
{
    public static event Action Defeated;

    public static void Raise()
    {
        Defeated?.Invoke();
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
public partial struct TowerDefeatSystem : ISystem
{
    bool _eventSent;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TowerHealth>();
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete();

        if (_eventSent || SystemAPI.GetSingleton<TowerHealth>().Current > 0)
            return;

        _eventSent = true;
        TowerDefeatEvent.Raise();
    }
}
