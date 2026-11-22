using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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

        var filter = new CollisionFilter
        {
            BelongsTo = 1u << 1,
            CollidesWith = (1u << 0) | (1u << 1),
            GroupIndex = 0
        };
        var collider = Unity.Physics.SphereCollider.Create(
            new SphereGeometry { Center = float3.zero, Radius = 0.72f }, filter);
        AddBlobAsset(ref collider, out _);
        AddComponent(entity, new PhysicsCollider { Value = collider });
        var mass = PhysicsMass.CreateDynamic(collider.Value.MassProperties, 1f);
        mass.InverseInertia = float3.zero;
        AddComponent(entity, mass);
        AddComponent(entity, new PhysicsVelocity());
        AddComponent(entity, new PhysicsGravityFactor { Value = 0f });
        AddSharedComponent(entity, PhysicsWorldIndex.Default);
    }
}
