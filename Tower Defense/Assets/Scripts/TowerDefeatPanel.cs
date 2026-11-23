using UnityEngine;

public class TowerDefeatPanel : MonoBehaviour
{
    [SerializeField] GameObject defeatPanel;

    void Awake()
    {
        defeatPanel.SetActive(false);
    }

    void OnEnable()
    {
        TowerDefeatEvent.Defeated += ShowDefeat;
    }

    void OnDisable()
    {
        TowerDefeatEvent.Defeated -= ShowDefeat;
    }

    void ShowDefeat()
    {
        defeatPanel.SetActive(true);
    }
}
