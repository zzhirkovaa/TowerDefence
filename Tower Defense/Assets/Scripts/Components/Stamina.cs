using Unity.Entities;

public struct Stamina : IComponentData
{
    public float Current;
    public float Max;
    public float DrainPerSecond;
    public float RegenPerSecond;
    public float MinToMove;
}
