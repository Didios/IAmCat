using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stimulus informations for Sight sense
/// containe position and accuracy of view
/// </summary>
public struct SightStimulus
{
    public Vector3 position;
    public float accuracy;
}

public class SenseSight : SenseBase<SightStimulus>
{
    [Header("View")]
    public float sightAngle = 60;
    public float distanceView = 10f;

    public float coneAccuracy = 0.5f;
    public float boundsMargin = 0.2f; // not test total width of the mesh, keep a margin

    // debug use only
    private List<Vector3> checkedPoints = new List<Vector3>();

    protected override void ResetSense()
    {
        base.ResetSense();

        if (showDebug)
        {
            checkedPoints.Clear();
        }
    }

    protected override bool DoSense(Transform obj, ref SightStimulus sti)
    {
        // check distance
        if ((obj.position - sensitivePart.position).sqrMagnitude > distanceView * distanceView)
        {
            return false;
        }

        // get object size and bounds
        Bounds b = GetObjectBounds(obj);
        float width = b.extents.x * (1 - boundsMargin);
        float depth = b.extents.z * (1 - boundsMargin);
        float height = b.extents.y * (1 - boundsMargin);

        // get object position and bbox
        var _right = obj.right * width;
        var _front = obj.forward * depth;
        var _up = obj.up * height;

        Vector3[] pointsToCheck =
        {
            b.center + _right,
            b.center - _right,
            b.center + _front,
            b.center - _front,
            b.center + _up,
            b.center - _up,

            b.center + _right + _front + _up,
            b.center + _right + _front - _up,
            b.center + _right - _front + _up,
            b.center + _right - _front - _up,
            b.center - _right + _front + _up,
            b.center - _right + _front - _up,
            b.center - _right - _front + _up,
            b.center - _right - _front - _up,

            b.center
        };

        // base stimulus
        sti.position = obj.position;
        sti.accuracy = 0;

        // detect if see each point, if yes, increment accuracy
        float accuracy_ratio = 1f / pointsToCheck.Length;
        checkedPoints.Clear();
        foreach (Vector3 point in pointsToCheck)
        {
            if (showDebug)
                checkedPoints.Add(point);

            if (CanSeePoint(point, obj))
            {
                sti.accuracy += accuracy_ratio;
            }
        }

        return sti.accuracy > 0;
    }

    /* get the global form of the object */
    private Bounds GetObjectBounds(Transform t)
    {
        Renderer[] rends = t.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds();
        foreach (Renderer r in rends)
        {
            if (b.size.sqrMagnitude == 0)
            {
                b = new Bounds(r.bounds.center, r.bounds.size);
            }
            else
            {
                b.Encapsulate(r.bounds);
            }
        }
        return b;
    }

    private bool CanSeePoint(Vector3 point, Transform parent)
    {
        Vector3 dirToPoint = point - sensitivePart.position;

        if (Vector3.Angle(sensitivePart.forward, dirToPoint) < sightAngle / 2.0f)
        {
            float distance = dirToPoint.magnitude;
            if (distance < distanceView)
            {
                RaycastHit hit;
                if (Physics.Raycast(sensitivePart.position, dirToPoint, out hit, distance))
                {
                    if (hit.transform.IsChildOf(parent) || hit.transform == parent)
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
        }

        return false;
    }


    public new void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (showDebug)
        {
            Vector3 pointMax = new Vector3(0, 0, distanceView);

            Gizmos.color = Color.red;
            Vector3 posWorld = sensitivePart.TransformPoint(pointMax);
            Gizmos.DrawLine(sensitivePart.position, posWorld);

            pointMax = Quaternion.AngleAxis(sightAngle / 2, Vector3.right) * pointMax;

            float nbRays = 100 * coneAccuracy;
            Quaternion rCone = Quaternion.AngleAxis(360 / nbRays, Vector3.forward);

            for (float i = 0; i < nbRays; i++)
            {
                pointMax = rCone * pointMax;
                posWorld = sensitivePart.TransformPoint(pointMax);
                Gizmos.color = Color.white;
                Gizmos.DrawLine(sensitivePart.position, posWorld);
            }

            // draw checked points
            Gizmos.color = Color.yellow;
            foreach (Vector3 p in checkedPoints)
            {
                Gizmos.DrawLine(sensitivePart.position, p);
            }
        }
    }
}
