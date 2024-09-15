using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Holdable : MonoBehaviour
{
    [SerializeField]
    private Vector3 m_heldPosition = new Vector3(0, 0, 0);

    [SerializeField]
    private Vector3 m_heldScale = new Vector3(1, 1, 1);

    [SerializeField]
    private Vector3 m_heldRotation = new Vector3(0, 0, 0);

    [System.NonSerialized]
    public bool m_isHeld = false;

    private Vector3 m_storedScale;

    public Rigidbody m_rigidbody;
    private Collider[] m_colliders;

    private void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_colliders = m_rigidbody.gameObject.GetComponents<Collider>();
    }

    private void Update()
    {
        if (m_isHeld)
        {
            transform.localPosition = m_heldPosition;
            transform.localScale = m_heldScale;
            transform.localRotation = Quaternion.Euler(m_heldRotation);
        }
    }

    public void Hold(Transform parent)
    {
        foreach (Collider collider in m_colliders)
        {
            collider.enabled = false;
        }

        m_rigidbody.useGravity = false;

        m_rigidbody.velocity = Vector3.zero;
        m_rigidbody.angularVelocity = Vector3.zero;

        m_storedScale = transform.localScale;

        transform.parent = parent;

        m_isHeld = true;
    }

    public void Release()
    {
        transform.parent = null;

        transform.localScale = m_storedScale;

        m_rigidbody.useGravity = true;

        foreach (Collider collider in m_colliders)
        {
            collider.enabled = true;
        }

        m_isHeld = false;
    }
}
