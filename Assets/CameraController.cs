using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    public enum Mode
    {
        FirstPerson,
        ThirdPerson
    }

    public static Mode activeMode;

    [SerializeField]
    public Camera m_camera;

    [SerializeField]
    public float m_sensitivity = 0.1f;

    [SerializeField]
    public Mode m_mode = Mode.FirstPerson;

    [Header("Third Person Settings")]

    [SerializeField]
    public float distance = 5f;

    [SerializeField]
    public Vector3 offset;

    private Controls m_controls;
    private Vector3 m_eulerAngles;
    private Vector3 m_rootPosition;

    private void OnEnable()
    {
        m_controls.Player.Enable();
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisable()
    {
        m_controls.Player.Disable();
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    }

    private void Awake()
    {
        m_controls = new Controls();

        m_controls.Player.Look.performed += OnLook;
    }

    private void Start()
    {
        m_rootPosition = m_camera.transform.localPosition;
        m_eulerAngles = new Vector3(m_camera.transform.eulerAngles.x, m_camera.transform.eulerAngles.y, 0f);
    }

    private void Update()
    {
        activeMode = m_mode;

        switch (m_mode)
        {
            case Mode.FirstPerson:
                m_camera.transform.localEulerAngles = new Vector3(m_eulerAngles.x, m_camera.transform.localEulerAngles.y, m_camera.transform.localEulerAngles.z);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, m_eulerAngles.y, transform.localEulerAngles.x);
                m_camera.transform.localPosition = m_rootPosition;
                break;
            case Mode.ThirdPerson:
                m_camera.transform.localPosition = m_rootPosition + offset + Quaternion.Euler(m_eulerAngles.x, 0, 0) * (Vector3.back * distance);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, m_eulerAngles.y, transform.localEulerAngles.x);
                m_camera.transform.LookAt(transform.TransformPoint(m_rootPosition + offset), Vector3.up);

                RaycastHit hit;
                Ray ray = new Ray(
                    transform.TransformPoint(m_rootPosition + offset),
                    Quaternion.Euler(m_eulerAngles.x, m_eulerAngles.y, 0) * Vector3.back
                    );
                float buffer = 0.5f;
                if (Physics.Raycast(ray, out hit, distance + buffer, ~LayerMask.GetMask("IgnoreRaycast")))
                {
                    float d = Mathf.Max(0f, hit.distance - buffer);
                    m_camera.transform.localPosition = m_rootPosition + offset + Quaternion.Euler(m_eulerAngles.x, 0, 0) * (Vector3.back * d);
                }

                break;
        }
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        Vector2 rotate = context.ReadValue<Vector2>() * m_sensitivity;

        m_eulerAngles.y += rotate.x;
        m_eulerAngles.x -= rotate.y;

        m_eulerAngles.x = Mathf.Clamp(m_eulerAngles.x, -89, 89);
    }
}
