using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stimulus informations for Hear sense
/// contain position and sound level
/// </summary>
public struct HearStimulus
{
    public Vector3 position;
    public float soundLevel;
}

public class SenseHear : SenseBase<HearStimulus>
{
    [Header("Hear")]
    public float distanceHear = 10;

    protected override bool DoSense(Transform obj, ref HearStimulus sti)
    {
        sti.position = obj.position;

        // check distance
        var _distance = Vector3.Distance(obj.position, sensitivePart.position);
        if (_distance < distanceHear)
        {
            Vector3 dirToPoint = obj.position - sensitivePart.position;

            var _percent = 1 - (_distance / distanceHear);

            var _noise = obj.GetComponent<NoiseStatus>();
            if (_noise != null && _noise.NoiseLevel > 0)
            {
                sti.soundLevel = _noise.NoiseLevel * _percent;
                return true;
            }
            else
            {
                sti.soundLevel = 0;
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
            Gizmos.DrawWireSphere(sensitivePart.position, distanceHear);
        }
    }
}
