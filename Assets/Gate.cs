using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gate : MonoBehaviour
{
    public Key m_key;

    private void OnCollisionEnter(Collision collision)
    {
        Key key = collision.gameObject.GetComponent<Key>();
        if (key != null && key.m_ID == m_key.m_ID)
        {
            Destroy(key.gameObject);
            Destroy(gameObject);
        }
    }
}
