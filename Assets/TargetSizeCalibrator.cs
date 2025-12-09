using UnityEngine;

public class TargetSizeCalibrator : MonoBehaviour
{
    [SerializeField] private Transform headset;
    [SerializeField] private Transform windshieldReference;
    [SerializeField] private Transform touchScreenReference;
    [SerializeField] private Transform steeringReference;
    [SerializeField] private float visualAngle = 0.3f;
    private float distanceFromWindshield;
    private float distanceFromTouchScreen;
    private float distanceFromSteering;

    public void CalculateDistances()
    {
        distanceFromTouchScreen = Vector3.Distance(touchScreenReference.position, headset.position); 
        distanceFromWindshield = Vector3.Distance(windshieldReference.position, headset.position);
        distanceFromSteering = Vector3.Distance(steeringReference.position, headset.position);
    }

    public float GetTargetSize(string condition)
    {
        float d = condition switch
        {
            "OnHand" => distanceFromSteering,
            "Windshield" => distanceFromWindshield,
            "TouchScreen" => distanceFromTouchScreen,
            _ => 0
        };
        // h = 2DTan(theta/2)   
        float height = 2 * d * Mathf.Tan(visualAngle * Mathf.Deg2Rad /2);
        return height;
    }

}