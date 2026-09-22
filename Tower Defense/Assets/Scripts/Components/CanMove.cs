using Unity.Entities;

/// <summary>
/// Выключаемый компонент: враг может идти по пути, только пока стамины хватает.
/// </summary>
public struct CanMove : IComponentData, IEnableableComponent
{
}
