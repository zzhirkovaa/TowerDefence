using Unity.Entities;
using UnityEngine;

public class TowerAuthoring : MonoBehaviour
{
}

class TowerBaker : Baker<TowerAuthoring>
{
    public override void Bake(TowerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TowerTarget>(entity);
    }
}
