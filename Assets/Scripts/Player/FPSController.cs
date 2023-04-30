using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    /* CONTROL TO SET
     * 0    Axis MOUSE_Y    front / back view axis
     * 1    Axis MOUSE_X    right / left view axis
     * 2    Axis FRONT      forward / backward move axis
     * 3    Axis SIDE       right / left move axis
     * 4    Button RUN      run button
     * 5    Button JUMP     jump button
     * 6    Button FIRE     fire / throw button
     * 7    Button GRAB     grab button
     * 8    Button PUNCH     punch button
     */

    #region Parameters
    [Header("Movement")]
    /* PUBLIC */
    public AnimationCurve speedCurve;
    public Interval speedCurveAcc = new Interval(0.02f, 0.1f);
    public float speedSide = 1; 
    public float speedBack = 1;
    public float jumpStrength = 8.0f;
    public float gravity = 20.0f;
    /* PUBLIC SCRIPT INFO */
    [HideInInspector] public Vector3 curSpeed = new Vector3();
    [HideInInspector] public bool canMove = true;
    [HideInInspector] public bool canJump = true;
    public bool isSpeedAnalog = false;
    /* PRIVATE */
    private float speedTime = 0;
    private CharacterController characterController;
    private Vector3 moveDir = Vector3.zero;
    private float rotX = 0;
    private float rotY = 0;
    private Vector3 forceToAdd = Vector3.zero;

    [Header("Shift")]
    public float speedReduction = 0.5f;
    public Vector3 scaleReduction = new Vector3(1, 0.5f, 1);
    private Vector3 scale;
    private bool hasShift = false;

    [Header("Camera")]
    public Camera playerCamera;
    public float cameraLookSpeed = 2.0f;
    public Vector2 cameraAngleLimit = new Vector2(360, 90);
    public AnimationCurve fovCurve; // go from 60 to 90 is great
    public bool canLockMouse = true;

    // "FEAR" when touch, character jump of fear
    [Header("Get Away From")]
    public string tagFear = "Fear";
    public Vector3 fearRelDir = new Vector3(-5, 10, 0);

    [Header("Grab")]
    /* PUBLIC */
    public float grabDistMax;
    public Vector3 grabRelPos;
    public Vector2 throwRelRot = new Vector2(0, 1);
    public float throwStrength = 5;
    /* PUBLIC SCRIPT INFO */
    [HideInInspector] public bool hasGrab = false;
    /* PRIVATE */
    private Quaternion grabRot;
    private Transform grabTransform;
    private Transform grabTransformParent;

    [Header("Fire")]
    public Transform bullet;
    public int bulletMax = 5;
    public Vector3 fireRelPos;
    public Vector2 fireRelRot = new Vector2(0, 1);
    public float fireStrength = 5;
    public float fireCd = 2;

    public bool canFireAndGrab = false;
    private float fireTimer = 0;
    private int bulletCount = 0;

    [Header("Punch")]
    public float distMax;
    public float strengthMax;
    public Animator punchAnim;

    public bool canPunchAndGrab = false;
    private float punchTimer = 0;

    [Header("Life")]
    public int hpMax = 1;
    [SerializeField] private int hp = 0;
    [SerializeField] private bool isDead
    {
        get { return (hp == 0); }
        set { }
    }

    [Header("Other")]
    public bool canFireActivate = true;
    public bool isFreeze = false;
    public bool showDebug = true;
    public bool activate = true;
    #endregion

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        hp = hpMax;
        scale = transform.localScale;
    }

    void Update()
    {
        if (activate && !isFreeze)
        {
            if (!isDead)
            {
                if (Cursor.visible && canLockMouse)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                if (Input.GetButton("ESCAPE"))
                {
                    activate = false;
                    return;
                }

                // detect player input
                bool isMoving = Input.GetAxis("FRONT") != 0 || Input.GetAxis("SIDE") != 0;

                bool isRunning = Input.GetButton("RUN") && isMoving;
                bool isJumping = Input.GetButton("JUMP");
                bool isShifting = Input.GetButton("SHIFT");

                bool isFiring = Input.GetButton("FIRE");
                bool isGrabbing = Input.GetButton("GRAB");
                bool isThrowing = Input.GetButton("FIRE");
                bool isPunching = Input.GetButton("PUNCH");

                #region Movement XYZ
                curSpeed = Vector3.zero;
                if (canMove)
                {
                    var _frontMov = Input.GetAxis("FRONT");
                    var _sideMov = Input.GetAxis("SIDE");
                    if (isSpeedAnalog)
                    {
                        if (_frontMov > 0)
                        {
                            playerCamera.fieldOfView = fovCurve.Evaluate(_frontMov);
                            curSpeed.x = speedCurve.Evaluate(_frontMov) * _frontMov;
                        }
                        else
                        {
                            playerCamera.fieldOfView = fovCurve.Evaluate(0);
                            curSpeed.x = speedBack * _frontMov;
                        }
                        curSpeed.z = speedSide * _sideMov;
                    }
                    else
                    {
                        if (_frontMov > 0)
                        {
                            if (isRunning && speedTime < 1)
                            {
                                speedTime += speedCurveAcc.max;
                            }
                            else if (!isRunning && speedTime > 0)
                            {
                                speedTime += speedCurveAcc.min;
                            }
                            if (speedTime > 1) speedTime = 1;
                            else if (speedTime < 0) speedTime = 0;

                            playerCamera.fieldOfView = fovCurve.Evaluate(speedTime);
                            curSpeed.x = speedCurve.Evaluate(speedTime) * _frontMov;
                        }
                        else
                        {
                            speedTime += speedCurveAcc.min * 2;
                            if (speedTime < 0) speedTime = 0;

                            playerCamera.fieldOfView = fovCurve.Evaluate(speedTime);
                            if (speedTime == 0)
                            {
                                curSpeed.x = speedBack * _frontMov;
                            }
                            else
                            {
                                curSpeed.x = speedCurve.Evaluate(speedTime) * _frontMov;
                            }
                        }
                        curSpeed.z = speedSide * _sideMov;
                    }
                }

                float movementDirY = moveDir.y;
                moveDir = (transform.forward * curSpeed.x) + (transform.right * curSpeed.z);

                if (isShifting)
                {
                    moveDir *= speedReduction;
                    if (!hasShift)
                    {
                        transform.localScale = new Vector3(
                            transform.localScale.x * scaleReduction.x,
                            transform.localScale.y * scaleReduction.y,
                            transform.localScale.z * scaleReduction.z);

                        hasShift = true;
                    }
                }
                else if (hasShift)
                {
                    transform.localScale = scale;
                    hasShift = false;
                }

                if (isJumping && canJump && characterController.isGrounded) moveDir.y = jumpStrength;
                else moveDir.y = movementDirY;

                if (!characterController.isGrounded)
                {
                    moveDir.y -= gravity * Time.deltaTime;
                }

                moveDir += forceToAdd;
                forceToAdd = Vector3.zero;

                characterController.Move(moveDir * Time.deltaTime);
                #endregion

                #region Grab
                if (isGrabbing)
                {
                    if (!hasGrab)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                                out hit, grabDistMax))
                        {
                            TransformInfos trans = hit.transform.GetComponent<TransformInfos>();
                            if (trans != null)
                            {
                                if (trans.canBeGrab)
                                {
                                    grabTransform = hit.transform;
                                    grabRot = grabTransform.rotation;
                                    trans.GrabIt();

                                    grabTransformParent = grabTransform.parent;
                                    grabTransform.parent = transform;

                                    grabTransform.position = transform.position
                                        + transform.forward * grabRelPos.x
                                        + transform.up * grabRelPos.y
                                        + transform.right * grabRelPos.z;

                                    hasGrab = true;
                                    if (showDebug) Debug.Log("[FPSController]:\n Grab");
                                }
                            }
                        }
                    }
                }

                // keep grab object visible in front
                if (hasGrab)
                {
                    grabTransform.localRotation = grabRot;

                    var _maxDist = Vector3.Distance(Vector3.zero, grabRelPos);

                    RaycastHit hit;
                    if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, _maxDist))
                    {
                        if (hit.transform != grabTransform)
                            grabTransform.position = hit.point;
                    }
                    else
                        grabTransform.position = GetRelPos(grabRelPos, true);

                    if (!canPunchAndGrab)
                        isPunching = false;
                }

                if (isThrowing && hasGrab)
                {
                    grabTransform.parent = grabTransformParent;

                    grabTransform.GetComponent<TransformInfos>().Unlock();
                    Throw(grabTransform, false);
                    hasGrab = false;

                    if (!canFireAndGrab)
                    {
                        isFiring = false;
                        fireTimer = 0.3f; // prevent from throw and fire by accident
                    }
                }
                #endregion

                #region Fire
                if (fireTimer > 0) { fireTimer -= Time.deltaTime; }

                if (isFiring && fireTimer <= 0
                    && bulletCount < bulletMax)
                {
                    Throw(bullet, true);
                    bulletCount += 1;

                    fireTimer = fireCd;
                }
                #endregion

                #region Punch
                if (punchTimer > 0) { punchTimer -= Time.deltaTime; }

                if (isPunching && punchTimer <= 0)
                {
                    // punch In-Game
                    RaycastHit hit;
                    if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, distMax))
                    {
                        var _force = -(strengthMax / distMax) * hit.distance + strengthMax;

                        var _breakable = hit.transform.GetComponent<ODestructive>();

                        if (_breakable != null)
                            _breakable.Contact();

                        if (hit.rigidbody != null)
                            hit.rigidbody.AddForce(playerCamera.transform.forward * _force, ForceMode.Impulse);

                        if (punchAnim != null)
                            punchAnim.SetTrigger("punch");

                        punchTimer = 0.1f;

                        if (showDebug) Debug.Log("[FPSController]:\n Punch: " + _force);
                    }
                }
                #endregion

                #region Camera Movement and Rotation
                if (canMove)
                {
                    rotY += -Input.GetAxis("MOUSE_Y") * cameraLookSpeed;
                    if (cameraAngleLimit.y < 360)
                        rotY = Mathf.Clamp(rotY, -cameraAngleLimit.y, cameraAngleLimit.y);
                    rotX += Input.GetAxis("MOUSE_X") * cameraLookSpeed;
                    if (cameraAngleLimit.x < 360)
                        rotX = Mathf.Clamp(rotX, -cameraAngleLimit.x, cameraAngleLimit.x);

                    playerCamera.transform.localRotation = Quaternion.Euler(rotY, 0, 0);
                    transform.rotation = Quaternion.Euler(0, rotX, 0);
                }
                #endregion
            }
        }
        else if (!isFreeze)
        {
            if (!Cursor.visible && canLockMouse)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            if (canFireActivate && Input.GetButton("FIRE"))
            {
                activate = true;
                fireTimer = 0.5f;
                return;
            }
        }
        else if (activate && isFreeze)
        {
            #region Camera Movement and Rotation
            if (canMove)
            {
                rotY += -Input.GetAxis("MOUSE_Y") * cameraLookSpeed;
                if (cameraAngleLimit.y < 360)
                    rotY = Mathf.Clamp(rotY, -cameraAngleLimit.y, cameraAngleLimit.y);
                rotX += Input.GetAxis("MOUSE_X") * cameraLookSpeed;
                if (cameraAngleLimit.x < 360)
                    rotX = Mathf.Clamp(rotX, -cameraAngleLimit.x, cameraAngleLimit.x);

                playerCamera.transform.localRotation = Quaternion.Euler(rotY, 0, 0);
                transform.rotation = Quaternion.Euler(0, rotX, 0);
            }
            #endregion
        }
    }

    public void AddForce(Vector3 force)
    {
        forceToAdd += force;
    }

    private void Throw(Transform obj, bool isBullet)
    {
        if (isBullet)
        {
            Transform proj = Instantiate<Transform>(bullet, GetRelPos(fireRelPos, true), new Quaternion());

            proj.GetComponent<Rigidbody>().AddForce(GetRelRot(fireRelRot, true) * fireStrength, ForceMode.Impulse);

            if (showDebug) Debug.Log("[FPSController]:\n Fire");
        }
        else
        {
            obj.GetComponent<Rigidbody>().AddForce(GetRelRot(throwRelRot, true) * throwStrength, ForceMode.Impulse);

            if (showDebug) Debug.Log("[FPSController]:\n Throw");
        }
    }

    public void ResetController()
    {
        if (hasGrab)
        {
            Destroy(grabTransform.gameObject);
            hasGrab = false;
        }

        hp = hpMax;

        bulletCount = 0;

        moveDir = Vector3.zero;
        forceToAdd = Vector3.zero;

        if (showDebug) Debug.Log("[FPSController]:\n Reset");
    }

    public void Damage(int _dmg)
    {
        if (_dmg > 0)
        {
            hp -= _dmg;

            if (hp < 0)
                hp = 0;

            if (showDebug) Debug.Log("[FPSController]:\n Damage");
        }
    }
    public void Heal(int _hl)
    {
        if (_hl > 0)
        {
            hp += _hl;

            if (hp > hpMax)
                hp = hpMax;

            if (showDebug) Debug.Log("[FPSController]:\n Heal");
        }
    }

    private Vector3 GetRelPos(Vector3 _relativePosition, bool toCameraLook)
    {
        Transform _base = transform;
        if (toCameraLook)
            _base = playerCamera.transform;

        Vector3 _worldPos = _base.position
            + _base.forward * _relativePosition.x
            + _base.up * _relativePosition.y
            + _base.right * _relativePosition.z;

        return _worldPos;
    }
    private Vector3 GetRelRot(Vector2 _angle, bool toCameraLook)
    {
        Transform _base = transform;
        if (toCameraLook)
            _base = playerCamera.transform;

        Vector3 _worldRot = _base.forward + Mathf.Tan(_angle.x) * _base.right
                + _base.forward + Mathf.Tan(_angle.y) * _base.up;

        return _worldRot.normalized;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag == tagFear)
        {
            AddForce(fearRelDir);

            if (showDebug) Debug.Log("[FPSController]:\n Fear");
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebug)
        {
            // FIRE
            Gizmos.color = Color.red;
            var _fire = GetRelPos(fireRelPos, true);
            Gizmos.DrawSphere(_fire, 0.2f);
            Gizmos.DrawLine(_fire, _fire + GetRelRot(fireRelRot, true) * fireStrength);

            // GRAB
            Gizmos.color = Color.blue;
            var _grab = GetRelPos(grabRelPos, true);
            Gizmos.DrawSphere(_grab, 0.2f);
            Gizmos.DrawLine(_grab, _grab + GetRelRot(throwRelRot, true) * throwStrength);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerCamera.transform.position,
                playerCamera.transform.position + playerCamera.transform.forward * grabDistMax);

            // PUNCH
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.forward * distMax);

            // FEAR
            Gizmos.color = Color.black;
            Gizmos.DrawLine(transform.position, GetRelPos(fearRelDir, false));

            // Raycast
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 0.6f);
        }
    }
}