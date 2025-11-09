using UnityEngine;

public class ResetCar : MonoBehaviour
{
    [SerializeField] private Transform car;

    public void Reset()
    {
        car.GetComponent<Rigidbody>().position = transform.position;
        car.GetComponent<Rigidbody>().rotation = transform.rotation;
        car.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }
}
