using Unity.Entities;
using UnityEngine;

/// <summary>
/// Маршрут волны. Точки — дочерние пустые объекты по порядку
/// или массив Waypoints в инспекторе. Последняя точка = башня.
/// </summary>
public class PathAuthoring : MonoBehaviour
{
    [Tooltip("Если пусто, берутся дочерние объекты сверху вниз")]
    public Transform[] Waypoints;

    public Transform[] GetOrderedWaypoints()
    {
        if (Waypoints != null && Waypoints.Length > 0)
            return Waypoints;

        var children = new Transform[transform.childCount];
        for (var i = 0; i < transform.childCount; i++)
            children[i] = transform.GetChild(i);

        return children;
    }

    void OnDrawGizmos()
    {
        var points = GetOrderedWaypoints();
        if (points == null || points.Length == 0)
            return;

        Gizmos.color = Color.yellow;
        for (var i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
                continue;

            Gizmos.DrawSphere(points[i].position, 0.25f);
            if (i > 0 && points[i - 1] != null)
                Gizmos.DrawLine(points[i - 1].position, points[i].position);
        }
    }
}

class PathBaker : Baker<PathAuthoring>
{
    public override void Bake(PathAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        var buffer = AddBuffer<PathWaypoint>(entity);

        var points = authoring.GetOrderedWaypoints();
        for (var i = 0; i < points.Length; i++)
        {
            var point = points[i];
            if (point == null)
                continue;

            DependsOn(point);
            buffer.Add(new PathWaypoint { Position = point.position });
        }
    }
}
