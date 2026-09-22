using Unity.Entities;

/// <summary>
/// Урон башне, когда враг дошёл до конца пути (классический «пропуск» в tower defense).
/// </summary>
public struct DamageOnGoal : IComponentData
{
    public float Value;
}
