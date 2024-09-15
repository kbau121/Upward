using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MovementController))]
[RequireComponent(typeof(CameraController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float m_maxGrappleForce = 30f;

    [SerializeField]
    private GameObject m_grapplePrefab;

    private enum AttachType
    {
        None,
        Terrain,
        Rigidbody
    }

    CameraController m_cameraController;
    MovementController m_movementController;

    Controls m_controls;
    float m_fire;

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
    }

    void Start()
    {
        m_movementController = GetComponent<MovementController>();
        m_cameraController = GetComponent<CameraController>();
    }

    void Update()
    {
        if (m_attachType != AttachType.None)
        {
            UpdateGrappleLine();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (m_grappledHoldable != null && collision.gameObject.GetInstanceID() == m_grappledHoldable.gameObject.GetInstanceID())
        {
            m_grappledHoldable.Hold(transform);
            ReleaseGrapple();
        }
    }

    void FixedUpdate()
    {
        float grappleForce = m_maxGrappleForce;

        switch (m_attachType)
        {
            case AttachType.Terrain:
                m_movementController.m_rigidbody.AddForce((m_grapplePosition - transform.position).normalized * grappleForce);
                break;
            case AttachType.Rigidbody:
                Vector3 worldGrapplePosition = m_grappledTransform.TransformPoint(m_grapplePosition);
                Vector3 pullDirection = (worldGrapplePosition - transform.position).normalized;

                float percentForce = m_movementController.m_rigidbody.mass / (m_grappledRigidbody.mass + m_movementController.m_rigidbody.mass);

                m_movementController.m_rigidbody.AddForce(pullDirection * grappleForce * (1 - percentForce), ForceMode.Acceleration);
                //m_grappledRigidbody.AddForce(-pullDirection * grappleForce * percentForce, ForceMode.Acceleration);
                m_grappledRigidbody.AddForceAtPosition(-pullDirection * grappleForce * percentForce, worldGrapplePosition, ForceMode.Acceleration);
                break;
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

    private void Fire()
    {
        if (m_grappledHoldable != null && m_grappledHoldable.m_isHeld)
        {
            m_grappledHoldable.Release();
            m_grappledHoldable.m_rigidbody.velocity = m_movementController.m_rigidbody.velocity;
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
                m_movementController.m_doFriction = false;
            }
            else
            {
                m_grappledTransform = hit.transform;
                m_grappledRigidbody = hit.rigidbody;
                m_grapplePosition = m_grappledTransform.InverseTransformPoint(hit.point);
                m_attachType = AttachType.Rigidbody;

                m_grappledHoldable = hit.transform.gameObject.GetComponent<Holdable>();

                m_grappleLine = Instantiate(m_grapplePrefab);
                m_movementController.m_doFriction = false;
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
        m_movementController.m_doFriction = true;
    }
}
