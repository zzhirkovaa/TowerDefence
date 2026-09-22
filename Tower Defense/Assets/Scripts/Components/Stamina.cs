using Unity.Entities;

/// <summary>
/// Стамина. Тратится на бег, плавно восстанавливается.
/// Если Current = 0, враг не может двигаться, пока не отдохнёт.
/// </summary>
public struct Stamina : IComponentData
{
    public float Current;
    public float Max;
    public float DrainPerSecond;
    public float RegenPerSecond;
    public float MinToMove;
}
