using System;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private float distance;
    [SerializeField] private float size;
    [SerializeField] private Transform cameraTransform;

    private void Update()
    {
        transform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y - distance, cameraTransform.position.z);
        transform.LookAt(cameraTransform);
        transform.Rotate(0, 180, 0);
        transform.localScale = new Vector3(size, size, size);
    }
}
