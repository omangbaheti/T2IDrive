using System;
using ubco.ovilab.HPUI.Legacy.utils;
using UnityEngine;
using UnityEngine.Serialization;

public class MainCamFollower : MonoBehaviour
{
    [SerializeField] private Transform anchorTransform;
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private Vector3 offset;
    [Header("One Euro Params")]
    private OneEuroFilter<Vector3> posFilter;
    [Header("SWD One Euro filter settings")]
    [Tooltip("Filter min cutoff for position filter")]
    [SerializeField] private float posFilterMinCutoff = 1f;
    [Tooltip("Beta value for position filter")]
    [SerializeField] private float posFilterBeta = 50;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (mainCameraTransform == null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        if (anchorTransform == null)
        {
            anchorTransform = Camera.main.transform;
        }
        posFilter = new OneEuroFilter<Vector3>(90, posFilterMinCutoff, posFilterBeta);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 offsetPos = anchorTransform.transform.right * offset.x + anchorTransform.transform.forward * offset.z + anchorTransform.transform.up * offset.y;
        transform.position = posFilter.Filter(anchorTransform.transform.position + offsetPos);
    }

    private void LateUpdate()
    {
        transform.LookAt(mainCameraTransform);
        transform.Rotate(0, 180, 0);
    }

    private void OnValidate()
    {
        posFilter = new OneEuroFilter<Vector3>(90, posFilterMinCutoff, posFilterBeta);
    }
}
