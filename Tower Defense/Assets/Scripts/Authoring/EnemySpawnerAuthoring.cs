using Unity.Entities;
using UnityEngine;

public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
}

class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        if (authoring.Prefab == null)
            return;

        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemySpawner
        {
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
        });
    }
}
