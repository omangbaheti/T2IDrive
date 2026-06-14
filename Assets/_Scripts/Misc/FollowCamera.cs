using System;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private float distance;
    [SerializeField] private float size;
    [SerializeField] private Transform cameraTransform;

    private void LateUpdate()
    {
        transform.rotation = cameraTransform.rotation;
        transform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y - distance, cameraTransform.position.z);
    }
}
