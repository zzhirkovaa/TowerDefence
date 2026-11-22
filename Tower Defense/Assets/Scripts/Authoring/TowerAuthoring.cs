using Unity.Entities;
using UnityEngine;

public class TowerAuthoring : MonoBehaviour
{
    public int MaxHealth = 100;
}

class TowerBaker : Baker<TowerAuthoring>
{
    public override void Bake(TowerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TowerTarget>(entity);
        var maxHealth = Mathf.Max(1, authoring.MaxHealth);
        AddComponent(entity, new TowerHealth { Current = maxHealth, Max = maxHealth });
    }
}
