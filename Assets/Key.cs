using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{
    private static int m_nextID = 0;

    [System.NonSerialized]
    public int m_ID;

    void Start()
    {
        m_ID = m_nextID++;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag.Equals("Finish"))
        {
            return;
        }

        LevelController.ActiveController.Restart();
    }
}
