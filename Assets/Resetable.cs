using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resetable : MonoBehaviour
{
    [SerializeField]
    private GameObject m_prefab;

    private Vector3 m_storedPosition;
    private Quaternion m_storedRotation;
    private Vector3 m_storedScale;
    private Transform m_storedParent;

    private Controls m_controls;

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

        m_controls.Player.Reset.performed += context => Reset();
    }

    void Start()
    {
        m_storedPosition = transform.localPosition;
        m_storedRotation = transform.localRotation;
        m_storedScale = transform.localScale;
        m_storedParent = transform.parent;
    }

    private void Reset()
    {
        GameObject gameObject = GameObject.Instantiate(m_prefab);
        gameObject.transform.parent = m_storedParent;
        gameObject.transform.localScale = m_storedScale;
        gameObject.transform.localRotation = m_storedRotation;
        gameObject.transform.localPosition = m_storedPosition;

        Destroy(this.gameObject);
    }
}
