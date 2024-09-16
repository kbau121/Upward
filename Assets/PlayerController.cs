using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Grappling Hook")]

    [SerializeField]
    private float m_maxGrappleForce = 100f;

    [SerializeField]
    private float m_maxGrappleLength = 10f;

    [SerializeField]
    private GameObject m_grapplePrefab;

    [SerializeField]
    private Text m_grappleCount;

    [SerializeField]
    private List<UnityEngine.UI.Image> m_crosshairs;

    [SerializeField]
    public int m_maxShotCount = 3;
    private int m_shotCount;

    private GrappleState m_grappleState;

    [Header("Movement")]

    [SerializeField]
    [Min(0f)]
    private float m_maxSpeed = 5f;

    [SerializeField]
    [Min(0f)]
    private float m_acceleration = 50f;

    [SerializeField]
    [Min(0f)]
    private float m_friction = 5f;

    [SerializeField]
    [Min(0f)]
    private float m_jumpSpeed = 5f;

    [SerializeField]
    private Collider m_groundCollider;

    [System.NonSerialized]
    public Rigidbody m_rigidbody;

    [System.NonSerialized]
    public bool m_isGrounded = false;

    [System.NonSerialized]
    public bool m_isNearGrounded = false;

    [System.NonSerialized]
    public bool m_doFriction = true;

    private enum AttachType
    {
        None,
        Terrain,
        Rigidbody
    }

    private enum GrappleState
    {
        Empty,
        OutsideRange,
        Ready,
        Active
    }

    CameraController m_cameraController;

    Controls m_controls;
    // Grappling Hook
    float m_fire;
    // Movement
    Vector2 m_move;
    bool m_jump;

    Vector3 m_grapplePosition;
    AttachType m_attachType;
    GameObject m_grappleLine;

    Transform m_grappledTransform;
    Rigidbody m_grappledRigidbody;
    Holdable m_grappledHoldable;

    private void OnEnable()
    {
        m_controls.Player.Enable();
    }

    private void OnDisable()
    {
        m_controls.Player.Disable();
    }

    private void Awake()
    {
        m_controls = new Controls();

        m_controls.Player.Fire.performed += context => Fire();
        m_controls.Player.Fire.canceled += context => Release();

        m_controls.Player.Move.performed += context => m_move = context.ReadValue<Vector2>();
        m_controls.Player.Move.canceled += context => m_move = Vector2.zero;

        m_controls.Player.Jump.performed += context => m_jump = true;
        m_controls.Player.Jump.canceled += context => m_jump = false;
    }

    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();

        m_cameraController = GetComponent<CameraController>();

        m_shotCount = m_maxShotCount;
    }

    void Update()
    {
        UpdateGrappleState();

        if (m_attachType != AttachType.None)
        {
            UpdateGrappleLine();
        }

        m_grappleCount.text = m_shotCount.ToString();
        foreach (UnityEngine.UI.Image image in m_crosshairs)
        {
            switch (m_grappleState)
            {
                case GrappleState.Active:
                    image.color = Color.blue;
                    break;
                case GrappleState.OutsideRange:
                    image.color = Color.black;
                    break;
                case GrappleState.Empty:
                    image.color = Color.red;
                    break;
                case GrappleState.Ready:
                    image.color = Color.green;
                    break;
            }
        }
    }

    Vector3 CalculateGrappleForce(float maxForce, Vector3 start, Vector3 end)
    {
        float grappleForce = maxForce;
        Vector3 gravityForce = Physics.gravity * m_rigidbody.mass;
        Vector3 pullDirection;
        float distance;

        distance = (m_grapplePosition - transform.position).magnitude;
        pullDirection = (m_grapplePosition - transform.position) / distance;
        Vector3 tangentialVelocity = m_rigidbody.velocity - Vector3.Project(m_rigidbody.velocity, pullDirection);

        float pullDotVel = Vector3.Dot(pullDirection, m_rigidbody.velocity.normalized);
        if (pullDotVel < 0f && distance > 5f)
        {
            // Increase pulling force to make a circular orbit
            float targetForce = m_rigidbody.mass * m_rigidbody.velocity.sqrMagnitude / distance;
            targetForce += Vector3.Project(gravityForce, pullDirection).magnitude;

            grappleForce = Mathf.Lerp(targetForce, grappleForce, -pullDotVel);
        }
        else
        {
            // Pull in towards the point
            distance = Mathf.Max(distance, 5f);

            float targetForce = m_rigidbody.mass * m_rigidbody.velocity.sqrMagnitude / distance;
            targetForce += Vector3.Project(gravityForce, pullDirection).magnitude;

            float tangentialSpeed = Mathf.Clamp(tangentialVelocity.magnitude, 1f, 5f);

            grappleForce = Mathf.Lerp(targetForce, grappleForce * 0.25f, Mathf.Clamp(pullDotVel * tangentialSpeed, 0f, 1f));

            if (m_isGrounded)
            {
                
            }
            else if (Vector3.Dot(-gravityForce.normalized, pullDirection.normalized) > 0)
            {
                grappleForce = grappleForce * Mathf.Lerp(0f, 1f, Vector3.Dot(-gravityForce.normalized, pullDirection.normalized));
            }
            else
            {
                grappleForce = grappleForce * Mathf.Lerp(0f, 1f, Mathf.Min(10f * Vector3.Dot(gravityForce.normalized, pullDirection.normalized), 1f));
            }
        }

        return pullDirection * grappleForce;
    }

    void FixedUpdate_GrapplingHook()
    {
        if (m_isGrounded) m_shotCount = m_maxShotCount;

        Vector3 grappleForce;

        switch (m_attachType)
        {
            case AttachType.Terrain:
                grappleForce = CalculateGrappleForce(m_maxGrappleForce, transform.position, m_grapplePosition);

                m_rigidbody.AddForce(grappleForce);

                break;
            case AttachType.Rigidbody:
                Vector3 worldGrapplePosition = m_grappledTransform.TransformPoint(m_grapplePosition);
                Vector3 pullDirection = (worldGrapplePosition - transform.position).normalized;

                grappleForce = m_maxGrappleForce * pullDirection * 0.5f;

                float percentForce = m_rigidbody.mass / (m_grappledRigidbody.mass + m_rigidbody.mass);

                m_rigidbody.AddForce(grappleForce * (1 - percentForce), ForceMode.Acceleration);
                m_grappledRigidbody.AddForceAtPosition(-grappleForce * percentForce, worldGrapplePosition, ForceMode.Acceleration);

                break;
        }
    }

    void FixedUpdate_Movement()
    {
        m_isGrounded = Physics.CheckCapsule(m_groundCollider.bounds.center, new Vector3(m_groundCollider.bounds.center.x, m_groundCollider.bounds.min.y, m_groundCollider.bounds.center.z), 0.1f, ~LayerMask.GetMask("Ignore Raycast"));
        m_isNearGrounded = Physics.CheckCapsule(m_groundCollider.bounds.center, new Vector3(m_groundCollider.bounds.center.x, m_groundCollider.bounds.min.y, m_groundCollider.bounds.center.z), 1f, ~LayerMask.GetMask("Ignore Raycast"));

        HandleMove();
        HandleJump();
    }

    void FixedUpdate()
    {
        FixedUpdate_GrapplingHook();
        FixedUpdate_Movement();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (m_grappledHoldable != null && collision.gameObject.GetInstanceID() == m_grappledHoldable.gameObject.GetInstanceID())
        {
            m_grappledHoldable.Hold(transform);
            ReleaseGrapple();
        }
    }

    private void UpdateGrappleLine()
    {
        switch (m_attachType)
        {
            case AttachType.Terrain:
                m_grappleLine.transform.position = (transform.position + m_grapplePosition) / 2f;
                m_grappleLine.transform.rotation = Quaternion.LookRotation(m_grapplePosition - transform.position);
                m_grappleLine.transform.localScale = new Vector3(1, 1, (m_grapplePosition - transform.position).magnitude);
                break;
            case AttachType.Rigidbody:
                Vector3 worldGrapplePosition = m_grappledTransform.TransformPoint(m_grapplePosition);
                m_grappleLine.transform.position = (transform.position + worldGrapplePosition) / 2f;
                m_grappleLine.transform.rotation = Quaternion.LookRotation(worldGrapplePosition - transform.position);
                m_grappleLine.transform.localScale = new Vector3(1, 1, (worldGrapplePosition - transform.position).magnitude);
                break;
        }
    }

    private void UpdateGrappleState()
    {
        if (m_attachType != AttachType.None)
        {
            m_grappleState = GrappleState.Active;
            return;
        }

        if (m_shotCount <= 0)
        {
            m_grappleState = GrappleState.Empty;
            return;
        }

        RaycastHit hit;
        Ray ray = new Ray(m_cameraController.m_camera.transform.position, m_cameraController.m_camera.transform.forward);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~LayerMask.GetMask("Ignore Raycast")))
        {
            m_grappleState = GrappleState.Ready;
            return;
        }

        m_grappleState = GrappleState.OutsideRange;
        return;
    }

    private void Fire()
    {
        if (m_grappledHoldable != null && m_grappledHoldable.m_isHeld)
        {
            m_grappledHoldable.Release();
            m_grappledHoldable.m_rigidbody.velocity = m_rigidbody.velocity;
            m_grappledHoldable.m_rigidbody.AddForce(m_cameraController.m_camera.transform.forward * 3f, ForceMode.Impulse);
            m_grappledHoldable = null;
        }
        else
        {
            FireGrapple();
        }
    }

    private void FireGrapple()
    {
        if (m_shotCount <= 0) return;
        if (!m_isGrounded) --m_shotCount;

        RaycastHit hit;
        Ray ray = new Ray(m_cameraController.m_camera.transform.position, m_cameraController.m_camera.transform.forward);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~LayerMask.GetMask("Ignore Raycast")))
        {
            if (hit.rigidbody == null)
            {
                m_grappledTransform = null;
                m_grapplePosition = hit.point;
                m_attachType = AttachType.Terrain;

                m_grappleLine = Instantiate(m_grapplePrefab);
                m_doFriction = false;
            }
            else
            {
                m_grappledTransform = hit.transform;
                m_grappledRigidbody = hit.rigidbody;
                m_grapplePosition = m_grappledTransform.InverseTransformPoint(hit.point);
                m_attachType = AttachType.Rigidbody;

                m_grappledHoldable = hit.transform.gameObject.GetComponent<Holdable>();

                m_grappleLine = Instantiate(m_grapplePrefab);
                m_doFriction = false;
            }
        }
    }

    private void Release()
    {
        ReleaseGrapple();
    }

    private void ReleaseGrapple()
    {
        m_attachType = AttachType.None;

        Destroy(m_grappleLine);
        m_doFriction = true;
    }

    private void HandleMove()
    {
        Vector3 horizontalVelocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z);
        float horizontalSpeed = horizontalVelocity.magnitude;

        Vector3 moveDir = transform.localToWorldMatrix * new Vector3(m_move.x, 0f, m_move.y);

        if (horizontalSpeed > m_maxSpeed && horizontalSpeed > 0)
        {
            Vector3 horizontalDir = horizontalVelocity / horizontalSpeed;
            float movementAllowance = Vector3.Dot(moveDir, horizontalDir);

            if (movementAllowance > 0)
            {
                moveDir -= movementAllowance * horizontalDir;
            }
        }

        if (m_isGrounded && m_doFriction)
        {
            m_rigidbody.AddForce(-horizontalVelocity * m_friction);
        }

        m_rigidbody.AddForce(moveDir * m_acceleration, ForceMode.Acceleration);
    }

    private void HandleJump()
    {
        if (m_jump && m_isGrounded)
        {
            m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, m_jumpSpeed, m_rigidbody.velocity.z);
        }
    }
}
