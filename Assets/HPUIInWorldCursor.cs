using System;
using UnityEngine;

public class HPUIInWorldCursor : MonoBehaviour
{
    [SerializeField] TraceVisualiser traceVisualiser;

    Transform sphere;
    private void Start()
    {
        traceVisualiser = FindObjectOfType<TraceVisualiser>();
       sphere = transform.GetChild(0);
    }

    private void Update()
    {
        Vector3 pos = new Vector3(traceVisualiser.currentPosition.x, 0f, traceVisualiser.currentPosition.y);
        sphere.localPosition = pos;
    }
}
