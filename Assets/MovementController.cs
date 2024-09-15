using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovementController : MonoBehaviour
{
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
    public bool m_doFriction = true;

    Controls m_controls;
    Vector2 m_move;
    bool m_jump;

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

        m_controls.Player.Move.performed += context => m_move = context.ReadValue<Vector2>();
        m_controls.Player.Move.canceled += context => m_move = Vector2.zero;

        m_controls.Player.Jump.performed += context => m_jump = true;
        m_controls.Player.Jump.canceled += context => m_jump = false;
    }

    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        m_isGrounded = Physics.CheckCapsule(m_groundCollider.bounds.center, new Vector3(m_groundCollider.bounds.center.x, m_groundCollider.bounds.min.y, m_groundCollider.bounds.center.z), 0.1f, ~LayerMask.GetMask("Ignore Raycast"));

        HandleMove();
        HandleJump();
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
