using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformInfos : MonoBehaviour
{
    [Header("Grab")]
    public bool canBeGrab = false;
    public bool isGrab = false;
    public float noiseOnGrab;
    public float noiseOnGrabTimer;

    [Header("Noise")]
    public AnimationCurve noiseCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public float noiseCurveTimer = 2;
    private NoiseStatus noiseCurrent;
    private float noiseToSet = 0;
    private float noiseMax = 1;

    private Rigidbody _rigidbody_;
    private Collider[] _collider_;

    [Header("Timers")]
    public float timer = 0;
    public float noiseTimer = 0;

    private void Start()
    {
        noiseCurrent = GetComponent<NoiseStatus>();
        if (noiseCurrent == null)
            noiseCurrent = gameObject.AddComponent<NoiseStatus>();

        _rigidbody_ = GetComponent<Rigidbody>();
        _collider_ = GetComponents<Collider>();
    }

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;

            noiseTimer += Time.deltaTime;
            if (noiseTimer > noiseCurveTimer)
            {
                noiseTimer = 0;
            }

            if (noiseCurve != null)
            {
                noiseToSet = noiseCurve.Evaluate(noiseTimer / noiseCurveTimer) * noiseMax;
            }
        }
        else
        {
            noiseToSet = 0;
        }

        noiseCurrent.NoiseLevel = noiseToSet;
    }

    public void SetNoise(float _noise, float _timer)
    {
        noiseTimer = 0;
        noiseToSet = _noise;
        noiseMax = _noise;
        timer = _timer;
    }

    public void GrabIt()
    {
        isGrab = true;
        SetNoise(noiseOnGrab, noiseOnGrabTimer);

        Lock();
    }

    public void Lock()
    {
        _rigidbody_.constraints = RigidbodyConstraints.FreezeAll;
        foreach (Collider c in _collider_) c.enabled = false;
    }
    public void Unlock()
    {
        _rigidbody_.constraints = RigidbodyConstraints.None;
        foreach (Collider c in _collider_) c.enabled = true;
    }
}
