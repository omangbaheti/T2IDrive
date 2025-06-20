using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using UnityEngine.Serialization;


namespace ubco.ovilab.ViconUnityStream
{
    public class ViconCoordinateSystemMerger : Singleton<ViconCoordinateSystemMerger>
    {

        public Transform ViconCoordinateSystem => viconCoordinateSystem;
        public Transform UnityCoordinateSystem => unityCoordinateSystem;

        [SerializeField] private Transform viconCoordinateSystem;
        [SerializeField] private Transform unityCoordinateSystem;

        [SerializeField] private SerializedDictionary<string, Transform> viconSubjects = new();
        [SerializeField] private SerializedDictionary<string, Transform> unityObjects = new();

        private void Start()
        {
            CustomSubjectScript[] viconSubjects = GetComponentsInChildren<CustomSubjectScript>();
            foreach (var subject in viconSubjects)
            {
                this.viconSubjects.Add(subject.SubejectName, subject.transform);
            }
        }

        public void RegisterObject(string subjectName, Transform unityObject)
        {
            unityObjects.Add(subjectName, unityObject);
        }

        [Button]
        public void MergeCoordinateSystems()
        {
            unityCoordinateSystem.transform.forward = viconCoordinateSystem.transform.forward;
            unityCoordinateSystem.transform.right = viconCoordinateSystem.transform.right;
            unityCoordinateSystem.transform.position = viconCoordinateSystem.transform.position;

            foreach (KeyValuePair<string, Transform> unityObject in unityObjects)
            {
                var viconSubject = viconSubjects[unityObject.Key];
                unityObject.Value.transform.rotation = viconSubject.transform.rotation;
                unityObject.Value.transform.position =  viconSubject.transform.position;
            }
        }
    }
}

