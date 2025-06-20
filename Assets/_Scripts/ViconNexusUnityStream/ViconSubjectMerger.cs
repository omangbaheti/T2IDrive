using UnityEngine;
using UnityEngine.Events;

namespace ubco.ovilab.ViconUnityStream.Utils
{
    public class ViconSubjectMerger : MonoBehaviour
    {
        public float DistanceThreshold => distanceThreshold;
        public float AngleThreshold => angleThreshold;
        public UnityEvent OnMergeSuccess => onMergeSuccess;
        public UnityEvent OnMergeFail => onMergeFail;
        public UnityEvent OnDifferenceAboveThreshold => onDifferenceAboveThreshold;
        
        [SerializeField] private string targetSubject;
        
        [Header("Thresholds")]
        [SerializeField] protected float distanceThreshold = 0.001f;
        [SerializeField] protected float angleThreshold = 0.5f;
        
        [Header("Merge Events")]
        [Tooltip("Called when the differences is above the respective thresholds."), SerializeField] 
        private UnityEvent onDifferenceAboveThreshold;
        [Tooltip("Called when failed to get the differences below the respective thresholds."), SerializeField] 
        private UnityEvent onMergeFail;
        [Tooltip("Called when successfully got the differences below the respective thresholds."), SerializeField] 
        private UnityEvent onMergeSuccess;
        
        private Transform Target
        {
            get
            {
                CustomSubjectScript target = ViconCoordinateSystemMerger.Instance.ViconSubjects[targetSubject];
                Debug.Assert(target != null, $"Target `{targetSubject}` not found. Make sure it is in the scene.");
                return target.transform;
            }
        }

        private void Start()
        {
            ViconCoordinateSystemMerger.Instance.RegisterObject(targetSubject, this);
        }

        public void MergeSubject()
        {
            transform.rotation = Target.transform.rotation;
            transform.position =  Target.transform.position;
        }

        public bool IsBelowThreshold()
        {
            return Vector3.Angle(transform.forward, Target.forward) < AngleThreshold && 
                   (transform.position - Target.position).magnitude < DistanceThreshold;
        }
        
        protected void Update()
        {
            if (!IsBelowThreshold())
            {
                Debug.LogWarning($"Target subject `{targetSubject}` is not below threshold");
                OnDifferenceAboveThreshold?.Invoke();
            }
        }
    }
}

