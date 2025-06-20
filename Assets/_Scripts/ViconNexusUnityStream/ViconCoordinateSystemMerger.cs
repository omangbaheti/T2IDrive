using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;


namespace ubco.ovilab.ViconUnityStream.Utils
{
    public class ViconCoordinateSystemMerger : Singleton<ViconCoordinateSystemMerger>
    {
        public Transform ViconCoordinateSystem => viconCoordinateSystem;
        public Transform UnityCoordinateSystem => unityCoordinateSystem;
        
        public SerializedDictionary<string, CustomSubjectScript> ViconSubjects => viconSubjects;
        public SerializedDictionary<string, ViconSubjectMerger> UnityObjects => unityObjects;
        
        /// <summary>
        /// Create a static vicon subject and attach it to the ground
        /// This becomes the Origin for the Vicon Coordinate System
        /// </summary>
        [Tooltip("Create a static Vicon subject and attach it to the ground" +
                 "This object becomes the Origin for the Vicon Coordinate System")]
        [SerializeField] private Transform viconCoordinateSystem;
        
        /// <summary>
        /// All objects driven by Vicon motion tracking should be placed under this transform
        /// </summary>
        [SerializeField] private Transform unityCoordinateSystem;

        [SerializeField] private SerializedDictionary<string, CustomSubjectScript> viconSubjects = new();
        [SerializeField] private SerializedDictionary<string, ViconSubjectMerger> unityObjects = new();
        
        [Tooltip("The distance threshold to achieve."), SerializeField]
        private float distanceThreshold = 0.001f;
        
        [Tooltip("The angle threshold to achieve"), SerializeField]
        private float angleThreshold = 0.5f;

        private void Start()
        {
            CustomSubjectScript[] viconSubjectsInScene = GetComponentsInChildren<CustomSubjectScript>();
            foreach (CustomSubjectScript subject in viconSubjectsInScene)
            {
                viconSubjects.Add(subject.SubejectName, subject);
            }
        }

        public void RegisterObject(string subjectName, ViconSubjectMerger unityObject)
        {
            unityObjects.Add(subjectName, unityObject);
        }

        [Button]
        public void MergeCoordinateSystems()
        {
            unityCoordinateSystem.transform.forward = viconCoordinateSystem.transform.forward;
            unityCoordinateSystem.transform.right = viconCoordinateSystem.transform.right;
            unityCoordinateSystem.transform.position = viconCoordinateSystem.transform.position;

            foreach (KeyValuePair<string, ViconSubjectMerger> unityObject in unityObjects)
            {
                unityObject.Value.MergeSubject();
            }
        }
    }
}

