using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Possible status of a sense
/// </summary>
public enum SenseStatus
{
    Enter,
    Stay,
    Leave
};

public abstract class SenseBase<Stimulus> : MonoBehaviour
{
    /* parent class for all Sense of an AI
     * Stimulus is a custom struct/class with goal to contain any useful informations (distance, sound level, luminosity, etc.
     */

    [Header("Base")]
    public Transform sensitivePart; // transform responsible for sense origin (position)

    // timer to refresh sense
    public float updateInterval = 0.3f;
    private float updateTimer = 0.0f;

    // object that have a relation with this sense
    protected List<Transform> trackedObjects = new List<Transform>(); // Object that can change sense status
    protected List<Transform> sensedObjects = new List<Transform>(); // Object detected by sense

    // callback for sense
    public delegate void SenseEventHandler(Stimulus sti, SenseStatus sta);
    private event SenseEventHandler CallSenseEvent;

    public bool showDebug = false;

    /// <summary>
    /// Sense refresh
    /// </summary>
    private void Update()
    {
        // refresh timer
        updateTimer += Time.deltaTime;
        if (updateTimer > updateInterval)
        {
            updateTimer = 0;

            Stimulus stimulus;
            SenseStatus sta;

            ResetSense();

            foreach (Transform t in trackedObjects)
            {
                stimulus = default(Stimulus);

                // check if sense trigger for this object
                if (DoSense(t, ref stimulus))
                {
                    sta = SenseStatus.Stay;

                    // add object if first sensation
                    if (!sensedObjects.Contains(t))
                    {
                        sensedObjects.Add(t);
                        sta = SenseStatus.Enter;
                    }

                    // callback
                    CallSenseEvent(stimulus, sta);
                }
                else
                {
                    // remove object if been sense precedently
                    if (sensedObjects.Contains(t))
                    {
                        sta = SenseStatus.Leave;
                        CallSenseEvent(stimulus, sta);
                        sensedObjects.Remove(t);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Make Sense action, is the sense trigger by a specific object
    /// if true, give the sense parameters in sti
    /// </summary>
    /// <param name="obj"> object to check </param>
    /// <param name="sti"> actual stimulus </param>
    /// <returns></returns>
    protected abstract bool DoSense(Transform obj, ref Stimulus sti);

    /// <summary>
    /// Reset Sense, can be redefine
    /// </summary>
    protected virtual void ResetSense() { }

    /// <summary>
    /// Add callback to sense
    /// </summary>
    /// <param name="handler"> event to trigger if sense is </param>
    public void AddSenseHandler(SenseEventHandler handler)
    {
        CallSenseEvent += handler;
    }

    /// <summary>
    /// Add object to track
    /// </summary>
    /// <param name="t"> object </param>
    public void AddObjectToTrack(Transform t)
    {
        trackedObjects.Add(t);
    }

    public void OnDrawGizmos()
    {
        if (showDebug)
        {
            Gizmos.color = Color.red;
            foreach (Transform t in sensedObjects)
            {
                Gizmos.DrawLine(sensitivePart.position, t.position);
            }
        }
    }
}
