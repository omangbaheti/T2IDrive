using ubco.ovilab.HPUI.Legacy.utils;
using UnityEngine;

public class SmoothedFollower : MonoBehaviour
{
    [SerializeField] private Transform followTarget;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private bool flipLookDirection;
    [SerializeField] private bool useLookAtAsOrientation;
    [Header("Offsets")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    public Transform FollowTarget { get => followTarget; set => followTarget = value; }

    private OneEuroFilter<Vector3> positionFilter;
    private bool validLookAtTarget;
    private bool validFollowTarget;

    private void Start()
    {
        if (followTarget == null)
        {
            Debug.LogWarning($"Follow target for {gameObject.name} is null. Will stay at origin.");
            transform.position = Vector3.zero;
        }
        else validFollowTarget = true;
        if (lookAtTarget == null)
        {
            Debug.LogWarning($"Look at target for {gameObject.name} is null. Will look at origin.");
            transform.forward = Vector3.zero - transform.position;
        }
        else validLookAtTarget = true;
        positionFilter = new(72);
    }

    private void Update()
    {
        if (validLookAtTarget && ((transform.position - lookAtTarget.position) != Vector3.zero))
        {
            if (useLookAtAsOrientation)
            {
                transform.forward = lookAtTarget.forward;
            }
            else
            {
                transform.forward = transform.position - lookAtTarget.position;
                transform.forward = flipLookDirection ? -transform.forward : transform.forward;
            }
        }
        if (validFollowTarget) transform.position = positionFilter.Filter(followTarget.position);
        // Apply offsets
        transform.position += transform.rotation * positionOffset; // offset in local space
        transform.rotation *= Quaternion.Euler(rotationOffset);   // apply rotation offset
    }
}