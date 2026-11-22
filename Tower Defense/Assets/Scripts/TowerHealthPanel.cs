using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class TowerHealthPanel : MonoBehaviour
{
    const int RepairAmount = 20;

    [SerializeField] Text healthText;
    [SerializeField] Button repairButton;

    World _world;
    EntityQuery _towerQuery;
    bool _queryCreated;
    bool _repairUsed;

    void Update()
    {
        if (!EnsureTower())
        {
            healthText.text = "Загрузка башни...";
            repairButton.interactable = false;
            return;
        }

        var tower = _towerQuery.GetSingletonEntity();
        var health = _world.EntityManager.GetComponentData<TowerHealth>(tower);
        healthText.text = health.Current == 0
            ? "Башня разрушена"
            : $"Здоровье башни: {health.Current}/{health.Max}";
        repairButton.interactable = !_repairUsed && health.Current > 0 && health.Current < health.Max;
    }

    bool EnsureTower()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (!_queryCreated || _world != world)
        {
            if (_queryCreated && _world != null && _world.IsCreated)
                _towerQuery.Dispose();
            _world = world;
            _towerQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<TowerTarget>(),
                ComponentType.ReadWrite<TowerHealth>());
            _queryCreated = true;
        }

        return _towerQuery.CalculateEntityCount() == 1;
    }

    // Called by the Button On Click event in the Inspector.
    public void RepairTower()
    {
        if (_repairUsed || !EnsureTower())
            return;

        var tower = _towerQuery.GetSingletonEntity();
        var health = _world.EntityManager.GetComponentData<TowerHealth>(tower);
        if (health.Current <= 0 || health.Current >= health.Max)
            return;

        health.Current = Mathf.Min(health.Max, health.Current + RepairAmount);
        _world.EntityManager.SetComponentData(tower, health);
        _repairUsed = true;
        healthText.text = $"Здоровье башни: {health.Current}/{health.Max}";
        repairButton.interactable = false;
    }

    void OnDestroy()
    {
        if (_queryCreated && _world != null && _world.IsCreated)
            _towerQuery.Dispose();
    }
}
