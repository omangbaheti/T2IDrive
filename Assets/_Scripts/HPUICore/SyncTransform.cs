using System;
using UnityEngine;

public class SyncTransform : MonoBehaviour
{
    public Transform target;
    public Vector3 posOffset;
    public Quaternion rotOffset;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.localScale = Vector3.one * 0.15f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        transform.position = target.position;
        transform.forward = -target.transform.up;
    }
}
