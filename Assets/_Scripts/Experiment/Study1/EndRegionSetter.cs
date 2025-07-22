using UnityEngine;
using UnityEngine.UI;

public class EndRegionSetter : MonoBehaviour
{
    [SerializeField] FingerRegions fingerRegions;
    private Button button;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(SetEndRegion);
    }

    void SetEndRegion()
    {
        ComfortStudyExperimentManager.Instance.endRegion = fingerRegions;
    }

}
