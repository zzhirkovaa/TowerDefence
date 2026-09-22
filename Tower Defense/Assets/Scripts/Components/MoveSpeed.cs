using Unity.Entities;

/// <summary>
/// Скорость перемещения entity (единиц в секунду).
/// </summary>
public struct MoveSpeed : IComponentData
{
    public float Value;
}
