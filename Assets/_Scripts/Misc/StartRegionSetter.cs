using UnityEngine;
using UnityEngine.UI;

public class StartRegionSetter : MonoBehaviour
{
    [SerializeField] FingerRegions fingerRegions;
    private Button button;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(SetStartRegion);
    }

    void SetStartRegion()
    {
        ComfortStudyExperimentManager.Instance.startRegion = fingerRegions;
    }

}