using Unity.Entities;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public float Speed = 3f;
    public float ReachDistance = 0.35f;
    public float MaxStamina = 2f;
    public float StaminaDrainPerSecond = 2.5f;
    public float StaminaRegenPerSecond = 2f;
}

class EnemyBaker : Baker<EnemyAuthoring>
{
    public override void Bake(EnemyAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new MoveSpeed
        {
            Value = authoring.Speed,
            ReachDistance = authoring.ReachDistance
        });
        AddComponent(entity, new Stamina
        {
            Current = authoring.MaxStamina,
            Max = authoring.MaxStamina,
            DrainPerSecond = authoring.StaminaDrainPerSecond,
            RegenPerSecond = authoring.StaminaRegenPerSecond,
            MinToMove = authoring.MaxStamina * 0.5f
        });
        AddComponent<CanMove>(entity);
        SetComponentEnabled<CanMove>(entity, true);
    }
}
