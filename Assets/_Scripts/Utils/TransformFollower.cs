using UnityEngine;

public class TransformFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Offset Settings")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private Vector3 scaleOffset = Vector3.one;
    
    [Header("Follow Options")]
    [SerializeField] private bool followPosition = true;
    [SerializeField] private bool followRotation = true;
    [SerializeField] private bool followScale = true;
    
    
    private Vector3 lastTargetLossyScale;
    
    private void Start()
    {
        if (target != null)
        {
            lastTargetLossyScale = target.lossyScale;
        }
    }
    
    private void LateUpdate()
    {
        UpdateTransform();
    }
    
    private void UpdateTransform()
    {
        if (target == null) return;
        
        if (followPosition)
        {
            Vector3 targetPosition = target.position + target.TransformDirection(positionOffset);
            transform.position = targetPosition;
        }
        
        if (followRotation)
        {
            Quaternion targetRotation = target.rotation * Quaternion.Euler(rotationOffset);
            transform.rotation = targetRotation;
        }
        
        if (followScale)
        {
            transform.localScale = scaleOffset;
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            lastTargetLossyScale = target.lossyScale;
        }
    }
    
    public void SetPositionOffset(Vector3 offset)
    {
        positionOffset = offset;
    }
    
    public void SetRotationOffset(Vector3 offset)
    {
        rotationOffset = offset;
    }
    
    public void SetScaleOffset(Vector3 offset)
    {
        scaleOffset = offset;
    }
    
    public void SnapToTarget()
    {
        if (target != null)
        {
            UpdateTransform();
        }
    }
    
    
}
