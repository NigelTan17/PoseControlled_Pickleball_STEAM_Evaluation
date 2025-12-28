// LockToGroundPlane.cs
using UnityEngine;

public class LockToGroundPlane : MonoBehaviour
{
    [Tooltip("Keep feet touching ground; tweak until shoes just kiss the court.")]
    public float modelYOffset = 0f;     // e.g. 0.05~0.12 depending on rig
    public float groundY = 0f;          // your court’s Y (looks like 0 from the screenshot)
    public bool freezePitchRoll = true; // keep upright if your solve tilts the hips

    void LateUpdate()
    {
        // clamp Y to ground
        var p = transform.position;
        p.y = groundY + modelYOffset;
        transform.position = p;

        // optional: keep character upright (don’t cancel yaw)
        if (freezePitchRoll)
        {
            var e = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, e.y, 0f);
        }
    }
}
