using Unity.Entities;
using UnityEngine;

/// <summary>
/// Башня игрока. Должна стоять в Sub Scene (Gameplay), иначе не запечётся в entity.
/// </summary>
public class TowerAuthoring : MonoBehaviour
{
    public float Health = 40f;
}

class TowerBaker : Baker<TowerAuthoring>
{
    public override void Bake(TowerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Renderable);
        AddComponent<TowerTag>(entity);
        AddComponent(entity, new Health
        {
            Current = authoring.Health,
            Max = authoring.Health
        });
    }
}
