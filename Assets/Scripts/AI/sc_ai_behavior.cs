using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Possible state of an ai
/// </summary>
public enum AiState
{
    Idle,       // wait some random time
    Walk,       // randomly walk, can idle
    Hunt,       // run after target
    Stunned,    // imobile and insensible
    Look,       // look around animation, then walk to position, and look around again
    Grab,       // go to the end, if player escape, stunned, arrived end game
    End,        // game is finish
};

[RequireComponent(typeof(sc_agent_controller))]
public class sc_ai_behavior : MonoBehaviour
{
    /* idle: go to random place inside of the map
     * if see player: 
     */

    private sc_agent_controller agent;
    private Animator animator;

    private AiState state = AiState.Idle;
    public bool isEnd
    {
        get { return state == AiState.End; }
        private set { }
    }

    [Header("Movement")]
    public float walkSpeed = 2;
    public float runSpeed = 5;
    public float lookSpeed = 3;
    [Space]
    public float distanceArrived = 0.1f;
    public float distanceMinIdle = 2;
    public float distanceMaxIdle = 10;
    [Header("States infos")]
    public float probaWalkToIdle = 0.5f;
    [Space]
    public float idleTimer = 5;
    public float idleTimerRandom = 2;
    private float timer = 0;
    [Space]
    public string stunTag;
    public float stunTimer = 5;
    private float timer_s = 0;
    [Space]
    public int escapeNeed = 1;
    public int escapeTime = 1;
    private int escapeCount = 0;
    private float timer_e = 0;
    public Vector3 escapeForce = new Vector3(1, 0, 1);
    public Vector3 grabPosition;
    [Header("Senses infos")]
    public float viewAccurracy = 0.5f;
    [Space]
    public float hearAccurracy = 1;
    private int lookCount = 0;
    [Header("Other")]
    public Vector3 endPosition;
    private Transform player;
    private Rigidbody playerRigidbody;
    private FPSController playerController;
    //TODO grab player if touched from front (hitbox)
    // if player grab, go to endPosition, if reached with player, end game

    //TODO stunned state when spit touch
    //TODO stop, look around and walk when hear sound
    [Space]
    public bool isAnimate = false;
    public bool showDebug = false;

    private void Start()
    {
        // get agent
        agent = GetComponent<sc_agent_controller>();
        agent.SetSpeed(walkSpeed);

        // get animation controller
        if (isAnimate && animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // get player data
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerRigidbody = player.GetComponent<Rigidbody>();
        playerController = player.GetComponent<FPSController>();

        // get sense
        var sight = GetComponent<SenseSight>();
        var touch = GetComponent<SenseTouch>();
        var hear = GetComponent<SenseHear>();

        // set sense callback and tracked object
        if (sight != null)
        {
            sight.AddSenseHandler(new SenseBase<SightStimulus>.SenseEventHandler(HandleSight));
            sight.AddObjectToTrack(player);
        }
        if (touch != null)
        {
            touch.AddSenseHandler(new SenseBase<TouchStimulus>.SenseEventHandler(HandleTouch));
            touch.AddObjectToTrack(player);
        }
        if (hear != null)
        {
            hear.AddSenseHandler(new SenseBase<HearStimulus>.SenseEventHandler(HandleHear));
            var _a_noise = GameObject.FindObjectsOfType<NoiseStatus>(true);
            foreach(NoiseStatus n in _a_noise)
            {
                hear.AddObjectToTrack(n.transform);
            }
        }
    }

    private void Update()
    {
        Debug.Log(state);
        if (state != AiState.End)
        {
            if (state == AiState.Idle)
            {
                timer -= Time.deltaTime;

                if (timer < 0)
                {
                    SetWalk();
                }
            }

            if (state == AiState.Walk && agent.IsArrived(agent.Target, distanceArrived))
            {
                if (Random.Range(0.0f, 1) <= probaWalkToIdle)
                {
                    SetIdle();
                }
                else
                {
                    agent.GoToRandom(distanceMinIdle, distanceMaxIdle);
                }
            }

            if (state == AiState.Look)
            {
                if (isAnimate)
                {
                    if (lookCount == 0 && animator.GetCurrentAnimatorStateInfo(0).IsName("look"))
                    {
                        lookCount += 1;
                    }
                    else if (lookCount == 1 && !animator.GetCurrentAnimatorStateInfo(0).IsName("look"))
                    {
                        agent.SetSpeed(lookSpeed);
                        if (agent.IsArrived(agent.Target, distanceArrived))
                        {
                            lookCount += 1;
                            animator.SetTrigger("look");
                        }
                    }
                    else if (lookCount == 2 && !animator.GetCurrentAnimatorStateInfo(0).IsName("look"))
                    {
                        lookCount = 0;
                        SetIdle();
                    }
                }
                else
                {
                    agent.SetSpeed(walkSpeed);
                    if (agent.IsArrived(agent.Target, distanceArrived))
                    {
                        SetIdle();
                    }
                }
            }

            if (state == AiState.Hunt && agent.IsArrived(agent.Target, distanceArrived))
            {
                SetIdle();
            }

            if (state == AiState.Stunned)
            {
                timer_s -= Time.deltaTime;
                if (timer_s < 0)
                {
                    SetIdle();
                }
            }

            if (state == AiState.Grab)
            {
                // move player to relative coordinate
                playerRigidbody.constraints = RigidbodyConstraints.None;
                var _pos = transform.forward * grabPosition.x +
                    transform.right * grabPosition.z +
                    transform.up * grabPosition.y;
                player.position = transform.position + _pos;
                playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                
                // deactivate player controller
                playerController.isFreeze = true;

                if (Input.GetButton("JUMP"))
                {
                    escapeCount++;
                    Debug.Log(escapeCount);
                }

                if (escapeCount > 0)
                {
                    timer_s += Time.deltaTime;
                    if (timer_s > escapeTime)
                    {
                        timer_s = 0;
                        escapeCount = 0;
                    }
                }

                if (agent.IsArrived(agent.Target, distanceArrived))
                {
                    state = AiState.End;
                }

                if (escapeCount >= escapeNeed)
                {
                    playerRigidbody.constraints = RigidbodyConstraints.None;
                    playerRigidbody.AddRelativeForce(escapeForce, ForceMode.Impulse);

                    playerController.isFreeze = false;

                    if (isAnimate)
                    {
                        animator.SetBool("grab", false);
                    }

                    SetStunned();
                }
            }
        }
    }
    
    private bool CanReceiveInfos()
    {
        return (state != AiState.Stunned && state != AiState.End && state != AiState.Grab);
    }
    
    public void OnCollisionEnter(Collision collision)
    {
        if (CanReceiveInfos())
        {
            if (collision.transform.tag == stunTag)
            {
                Destroy(collision.gameObject);
                SetStunned();
            }
            else if (collision.gameObject == player.gameObject)
            {
                SetGrab();
            }
        }
    }

    #region handle sense
    private void HandleSight(SightStimulus sti, SenseStatus evt)
    {
        if (!CanReceiveInfos())
        {
            return;
        }

        /* infos to change when event (here sight) call, means that the status of sti has change */
        if (evt == SenseStatus.Enter && sti.accuracy >= viewAccurracy)
        {
            SetHunt();
            agent.GoTo(sti.position);
        }
        else if (evt == SenseStatus.Stay)
        {
            SetHunt();
            agent.GoTo(sti.position);
        }
    }
    private void HandleTouch(TouchStimulus sti, SenseStatus evt)
    {
        if (!CanReceiveInfos())
        {
            return;
        }

        /* infos to change when event (here sight) call, means that the status of sti has change */
        if (evt == SenseStatus.Enter || evt == SenseStatus.Stay)
        {
            SetHunt();
            agent.GoTo(sti.position);
        }
    }
    private void HandleHear(HearStimulus sti, SenseStatus evt)
    {
        if (!CanReceiveInfos())
        {
            return;
        }

        /* infos to change when event (here sight) call, means that the status of sti has change */
        if (evt == SenseStatus.Enter && sti.soundLevel >= hearAccurracy)
        {
            SetLook();
            agent.GoTo(sti.position);
        }
    }
    #endregion

    #region Set State
    private void SetIdle()
    {
        state = AiState.Idle;
        agent.SetSpeed(0);

        timer = idleTimer + Random.Range(-idleTimerRandom, idleTimerRandom);
    }

    private void SetWalk()
    {
        state = AiState.Walk;
        agent.SetSpeed(walkSpeed);
        agent.GoTo(transform.position);
    }

    private void SetHunt()
    {
        state = AiState.Hunt;
        agent.SetSpeed(runSpeed);
        agent.GoTo(transform.position);
    }

    private void SetLook()
    {
        state = AiState.Look;
        agent.SetSpeed(0);
        agent.GoTo(transform.position);

        lookCount = 0;
        if (isAnimate)
        {
            animator.SetTrigger("look");
        }
    }

    private void SetStunned()
    {
        state = AiState.Stunned;
        agent.SetSpeed(0);
        agent.GoTo(transform.position);

        timer_s = stunTimer;
        if (isAnimate)
        {
            animator.SetTrigger("stunned");
        }
    }

    private void SetGrab()
    {
        state = AiState.Grab;
        agent.SetSpeed(walkSpeed);
        agent.GoTo(endPosition);

        escapeCount = 0;
        timer_e = 0;

        if (isAnimate)
        {
            animator.SetBool("grab", true);
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (showDebug)
        {
            // end position
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(endPosition, distanceArrived);

            // grab position
            var _pos = transform.forward * grabPosition.x +
                transform.right * grabPosition.z +
                transform.up * grabPosition.y;
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position + _pos, 0.1f);

            // release force
            var _force = transform.forward * escapeForce.x +
                transform.right * escapeForce.z +
                transform.up * escapeForce.y;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + _pos, transform.position + _pos + _force);

        }
    }
}
