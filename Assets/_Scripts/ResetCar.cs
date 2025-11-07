using UnityEngine;

public class ResetCar : MonoBehaviour
{
    [SerializeField] private Transform car;

    public void Reset()
    {
        car.GetComponent<Rigidbody>().position = transform.position;
    }
}
