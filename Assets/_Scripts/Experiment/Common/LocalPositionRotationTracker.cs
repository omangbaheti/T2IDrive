using System;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UXF
{
    /// <summary>
    /// Attach this component to a gameobject and assign it in the trackedObjects field in an ExperimentSession to automatically record position/rotation of the object at each frame.
    /// </summary>
    public class LocalPositionTracker : PositionRotationTracker
    {
        private void Awake()
        {
            if (String.IsNullOrWhiteSpace(objectName))
            {
                objectName = gameObject.name;
            }
        }

        public override string MeasurementDescriptor => "movement";
        public override IEnumerable<string> CustomHeader => new[]
        {
            "local_pos_x", "local_pos_y", "local_pos_z",
            "global_pos_x", "global_pos_y", "global_pos_z",
            "rot_x_quaternion", "rot_y_quaternion", "rot_z_quaternion", "rot_w_quaternion"
        };

        /// <summary>
        /// Returns current position and rotation values
        /// </summary>
        /// <returns></returns>
        protected override UXFDataRow GetCurrentValues()
        {
            // get position and rotation
            Vector3 p = gameObject.transform.localPosition;
            Vector3 p_global = gameObject.transform.position;
            Quaternion r = gameObject.transform.rotation;
            Vector3 r_euler = r.eulerAngles;

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
            };

            return values;
        }
    }
}