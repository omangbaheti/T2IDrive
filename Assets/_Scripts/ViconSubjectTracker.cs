using System;
using UnityEngine;


namespace ubco.ovilab.ViconUnityStream
{
    public class ViconSubjectTracker : MonoBehaviour
    {

        [SerializeField] private string subjectName;

        private void Start()
        {
            ViconCoordinateSystemMerger.Instance.RegisterObject(subjectName, transform);
        }
    }
}

