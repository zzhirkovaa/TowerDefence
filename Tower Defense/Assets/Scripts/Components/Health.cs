using Unity.Entities;

/// <summary>
/// Здоровье. Если Current меньше или равно 0 — DeathSystem уничтожает entity
/// (для башни вместо этого включается GameOver).
/// </summary>
public struct Health : IComponentData
{
    public float Current;
    public float Max;
}
