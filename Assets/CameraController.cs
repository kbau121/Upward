using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    public Camera m_camera;

    [SerializeField]
    public float m_sensitivity = 0.1f;

    Controls m_controls;

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

    private void OnLook(InputAction.CallbackContext context)
    {
        Vector2 rotate = context.ReadValue<Vector2>() * m_sensitivity;

        transform.Rotate(transform.up, rotate.x, Space.World);
        m_camera.transform.Rotate(transform.right, -rotate.y, Space.World);

        if (m_camera.transform.eulerAngles.z > 90f)
        {
            if (m_camera.transform.eulerAngles.x < 90f)
            {
                m_camera.transform.eulerAngles = new Vector3(90f, m_camera.transform.eulerAngles.y, m_camera.transform.eulerAngles.z);
            }
            else if (m_camera.transform.eulerAngles.x > 270f)
            {
                m_camera.transform.eulerAngles = new Vector3(270f, m_camera.transform.eulerAngles.y, m_camera.transform.eulerAngles.z);
            }
        }
    }
}
