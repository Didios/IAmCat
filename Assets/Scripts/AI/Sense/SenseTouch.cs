using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stimulus informations for Touch sense
/// contain position
/// </summary>
public struct TouchStimulus
{
    public Vector3 position;
}

public class SenseTouch : SenseBase<TouchStimulus>
{
    [Header("Touch")]
    public float distanceTouch = 2;

    protected override bool DoSense(Transform obj, ref TouchStimulus sti)
    {
        sti.position = obj.position;

        if ((obj.position - sensitivePart.position).sqrMagnitude < distanceTouch * distanceTouch)
        {
            Vector3 dirToPoint = obj.position - sensitivePart.position;

            RaycastHit hit;
            if (Physics.Raycast(sensitivePart.position, dirToPoint, out hit, distanceTouch))
            {
                if (hit.transform.IsChildOf(obj) || hit.transform == obj)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    public void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (showDebug)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(sensitivePart.position, distanceTouch);
        }
    }
}
