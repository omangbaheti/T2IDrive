using System.Collections.Generic;
using UnityEngine;
using UXF;

public class CustomThumbPositionTracker : LocalPositionTracker
{
    public float CumulativeDistance => cumulativeDistanceTravelled;
    public float NetDisplacement => netDisplacement;
    [SerializeField] List<Vector3> thumbPositions = new List<Vector3>();
    [SerializeField] List<Quaternion> thumbRotations = new List<Quaternion>();
    [SerializeField] float cumulativeDistanceTravelled = 0f;
    [SerializeField] private float netDisplacement = 0f;
    [SerializeField] private Transform handRoot;
    public override string MeasurementDescriptor => "movement";
    public override IEnumerable<string> CustomHeader => new[]
    {
        "local_pos_x", "local_pos_y", "local_pos_z",
        "global_pos_x", "global_pos_y", "global_pos_z",
        "rot_x_quaternion", "rot_y_quaternion", "rot_z_quaternion", "rot_w_quaternion",
        "cumulative_distance", "net_displacement"
    };

    public void GestureStarted()
    {
        cumulativeDistanceTravelled = 0f;
        netDisplacement = 0f;
        var currentThumbPosition = handRoot.InverseTransformPoint(transform.position);
        thumbPositions.Add(currentThumbPosition);
        thumbRotations.Add(transform.rotation);
        RecordRow();
    }

    public void GestureOngoing()
    {
        //Redo: Transform position with respect to the wrist
        var currentThumbPosition = handRoot.InverseTransformPoint(transform.position);
        thumbPositions.Add(currentThumbPosition);
        thumbRotations.Add(transform.rotation);
        cumulativeDistanceTravelled += Vector3.Distance(currentThumbPosition, thumbPositions[^2]);
        netDisplacement = Vector3.Distance(currentThumbPosition, thumbPositions[0]);
        RecordRow();
    }

    public void GestureCancelled()
    {
        RecordRow();
        thumbPositions.Clear();
        thumbRotations.Clear();
    }

    public void GestureCompleted()
    {
        var currentThumbPosition = handRoot.InverseTransformPoint(transform.position);
        thumbPositions.Add(currentThumbPosition);
        thumbRotations.Add(transform.rotation);
        cumulativeDistanceTravelled += Vector3.Distance(thumbPositions[^1], thumbPositions[^2]);
        netDisplacement = Vector3.Distance(thumbPositions[^1], thumbPositions[0]);
        RecordRow();
        thumbPositions.Clear();
        thumbRotations.Clear();
    }

    protected override UXFDataRow GetCurrentValues()
    {
        // get position and rotation
        Vector3 p = gameObject.transform.localPosition;
        Vector3 p_global = gameObject.transform.position;
        Quaternion r = gameObject.transform.rotation;
        float d = netDisplacement;
        float cd = cumulativeDistanceTravelled;
        // return position, rotation (x, y, z) as an array
        var values = new UXFDataRow()
        {
            ("local_pos_x", p.x),
            ("local_pos_y", p.y),
            ("local_pos_z", p.z),
            ("global_pos_x", p_global.x),
            ("global_pos_y", p_global.y),
            ("global_pos_z", p_global.z),
            ("rot_x_quaternion", r.x),
            ("rot_y_quaternion", r.y),
            ("rot_z_quaternion", r.z),
            ("rot_w_quaternion", r.w),
            ("cumulative_distance", cd),
            ("net_displacement", d)
        };

        return values;
    }

}
