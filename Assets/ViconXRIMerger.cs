using System;
using System.Collections.Generic;
using EditorAttributes;
using ubco.ovilab.ViconUnityStream;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;

public class ViconXRIMerger : MonoBehaviour
{
    [SerializeField] private Transform viconOrigin;
    [SerializeField] private Transform viconToUnityOrigin;
    [SerializeField] private List<ViconAndUnityObjectContainer> viconToUnityObjects = new();
    [SerializeField] private bool showGizmos = false;

    [Button]
    public void Merge()
    {
        foreach (ViconAndUnityObjectContainer container in viconToUnityObjects)
        {
            MergeObject(container);
        }
    }

    public void MergeObject(ViconAndUnityObjectContainer container)
    {
        Transform viconObject = container.viconObject;
        Transform offsetTransform = container.offsetTransform;
        Transform unityObject = container.unityObject;
        offsetTransform.localRotation = Quaternion.identity;
        offsetTransform.localPosition = Vector3.zero;

        if (container.unityObject.name != "Hand")
        {
            Quaternion localXRRotRelToParent = Quaternion.Inverse(viconToUnityOrigin.rotation) * unityObject.rotation;
            Quaternion localViconRotRelToParent = Quaternion.Inverse(viconOrigin.rotation) * viconObject.rotation;
            offsetTransform.localRotation = localViconRotRelToParent * Quaternion.Inverse(localXRRotRelToParent);
        }
        else
        {
            offsetTransform.localRotation = Quaternion.Euler(0, -viconOrigin.rotation.eulerAngles.y, 0);
            Debug.Log("Angle:" + viconOrigin.rotation.eulerAngles.y);
        }
        Vector3 localXRPosRelToParent = viconToUnityOrigin.InverseTransformPoint(unityObject.position);
        Vector3 localViconPosRelToParent = viconOrigin.InverseTransformPoint(viconObject.position);
        offsetTransform.localPosition = localViconPosRelToParent - localXRPosRelToParent;

        if (container.unityObject.name == "Hand")
        {
            container.viconObject.GetComponent<CustomHandScript>().XRIOffset.position = offsetTransform.localPosition;
            container.viconObject.GetComponent<CustomHandScript>().XRIOffset.rotation= offsetTransform.localRotation;
        }

    }

    public void Update()
    {
        // foreach (ViconAndUnityObjectContainer container in viconToUnityObjects)
        // {
        //     if (container.viconObject.name == "TabletChild")
        //     {
        //         MergeObject(container);
        //     }
        // }
    }


    private void OnDrawGizmos()
    {
        if(!showGizmos) return;
        Handles.color = Color.red;

        foreach (ViconAndUnityObjectContainer objectPair in viconToUnityObjects)
        {
            Handles.SphereHandleCap(0,objectPair.unityObject.position, objectPair.unityObject.rotation, 0.05f, EventType.Repaint);

            Vector3 pos = objectPair.unityObject.position;
            Quaternion rot = objectPair.unityObject.rotation;

            // Define direction vectors
            Vector3 forward = rot * Vector3.forward;
            Vector3 up = rot * Vector3.up;
            Vector3 right = rot * Vector3.right;

            float lineLength = 0.3f;

            // Draw forward (blue)
            Handles.color = Color.blue;
            Handles.DrawLine(pos, pos + forward * lineLength);

            // Draw up (green)
            Handles.color = Color.green;
            Handles.DrawLine(pos, pos + up * lineLength);

            // Draw right (red)
            Handles.color = Color.red;
            Handles.DrawLine(pos, pos + right * lineLength);
        }
    }
}

[Serializable]
public class ViconAndUnityObjectContainer
{
    [SerializeField] public Transform viconObject;
    [SerializeField] public Transform offsetTransform;
    [SerializeField] public Transform unityObject;
}
