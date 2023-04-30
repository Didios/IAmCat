using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class ODestructive : MonoBehaviour
{
    [Header("Break Params")]
    [SerializeField] private List<string> tags = new List<string>();
    [SerializeField] private float maxSpeed;

    [Header("After Break")]
    [SerializeField] private GameObject breaked_GO;
    [SerializeField] private Material[] breaked_M = new Material[0];

    [Header("How to Break")]
    [SerializeField] private bool breakOnTag = false;
    [SerializeField] private bool breakOnContact = false;
    [SerializeField] private bool breakOnCollision = false;
    [SerializeField] private bool breakOnPhysics = false;

    [Header("Gameplay")]
    [SerializeField] public int weight = 1;
    [SerializeField] private float noiseOnBreak;
    [SerializeField] private float noiseTime;
    [SerializeField] private bool hasGameplay = false;

    [Space]
    [SerializeField] private float timerInvulnerable = 0.5f;
    public bool toBreak = false;
    public bool hasBreak { get; private set; }

    public UnityEvent<int> breakEvent { get; private set; }
    private List<TransformInfos> transformInfos = new List<TransformInfos>();
    private Outline outline;
    private Rigidbody _rigidBody;

    private void Start()
    {
        if (breakEvent == null)
            breakEvent = new UnityEvent<int>();

        outline = GetComponent<Outline>();

        _rigidBody = GetComponent<Rigidbody>();
        if (_rigidBody == null && breakOnPhysics)
            _rigidBody = gameObject.AddComponent<Rigidbody>();

        if (breaked_GO != null)
        {
            transformInfos = breaked_GO.GetComponentsInChildren<TransformInfos>().ToList();
            if (transformInfos.Count == 0)
            {
                for (int i = 0; i < breaked_GO.transform.childCount; i++)
                    transformInfos.Add(breaked_GO.transform.GetChild(i).gameObject.AddComponent<TransformInfos>());
            }
        }
        else
        {
            var _TI = GetComponent<TransformInfos>();
            if (_TI == null)
                _TI = gameObject.AddComponent<TransformInfos>();
            transformInfos.Add(_TI);
        }

        if (breaked_GO != null)
            breaked_GO.SetActive(false);
    }

    private void Update()
    {
        if (timerInvulnerable > 0)
            timerInvulnerable -= Time.deltaTime;

        if (toBreak)
        {
            Break();
            toBreak = false;
        }
    }

    public void Break()
    {
        if (!hasBreak && timerInvulnerable <= 0)
        {
            if (breaked_GO != null)
            {
                breaked_GO.transform.SetParent(transform.parent);
                transform.SetParent(breaked_GO.transform);

                // better than Instantiate
                breaked_GO.SetActive(true);

                // set velocity to make illusion of beeing the same object
                var _breakedRigid = breaked_GO.GetComponent<Rigidbody>();

                if (_rigidBody != null)
                {
                    if (_breakedRigid != null)
                        _breakedRigid.velocity = _rigidBody.velocity;
                    else
                    {
                        foreach (Rigidbody _childRigid in breaked_GO.GetComponentsInChildren<Rigidbody>())
                        {
                            if (_childRigid != null)
                                _childRigid.velocity = _rigidBody.velocity;
                        }
                    }
                }

                // better than Destroy()
                gameObject.SetActive(false);
            }
            else
            {
                if (breaked_M.Length != 0)
                    GetComponent<MeshRenderer>().materials = breaked_M;

                // not destroy
                outline.enabled = false;
                this.enabled = false;
            }

            // set noise
            if (hasGameplay)
            {
                foreach (TransformInfos _TI in transformInfos)
                    _TI.SetNoise(noiseOnBreak, noiseTime);
            }

            hasBreak = true;
            breakEvent.Invoke(weight);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (breakOnTag)
        {
            if (tags.Contains(collision.transform.tag))
                Break();
        }

        if (breakOnCollision)
            Break();

        if (breakOnPhysics)
        {
            if (_rigidBody.velocity.magnitude > maxSpeed)
                Break();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (breakOnTag)
        {
            if (tags.Contains(other.transform.tag))
                Break();
        }
    }

    public void Contact()
    {
        if (breakOnContact)
            Break();
    }
}
