using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Одна точка маршрута. Лежит в DynamicBuffer на entity пути.
/// Последняя точка — цель (башня игрока).
/// </summary>
public struct PathWaypoint : IBufferElementData
{
    public float3 Position;
}
