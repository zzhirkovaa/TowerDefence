using Unity.Entities;

/// <summary>
/// Состояние движения по маршруту: какой waypoint сейчас цель.
/// </summary>
public struct PathFollower : IComponentData
{
    public int WaypointIndex;
    public float ReachDistance;
}
