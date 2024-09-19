using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    public static LevelController ActiveController;

    [SerializeField]
    public Animator m_animator;

    [SerializeField]
    private Text m_stopwatch;

    [SerializeField]
    private Text m_personalBest;

    private float m_time;

    private bool m_hasBegun;
    private bool m_active;
    private bool m_hasFinished;

    private Controls m_controls;

    private void Start()
    {
        ActiveController = this;

        m_controls = new Controls();

        m_time = 0;

        m_active = false;
        m_hasBegun = false;
        m_hasFinished = false;

        TimeSpan t = TimeSpan.FromSeconds(m_time);
        m_stopwatch.text = t.ToString(@"mm\:ss\:fff");

        float personalBest = PlayerPrefs.GetFloat("personal_best", -1f);
        if (personalBest < 0f)
        {
            m_personalBest.text = "";
        }
        else
        {
            TimeSpan pb = TimeSpan.FromSeconds(personalBest);
            m_personalBest.text = pb.ToString(@"mm\:ss\:fff");
        }
    }

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

        m_controls.Player.Fire.performed += context => Begin();
        m_controls.Player.Fire.performed += context => TryEnd();
        m_controls.Player.Move.performed += context => Begin();
        m_controls.Player.Jump.performed += context => Begin();
        m_controls.Player.Reset.performed += context => Restart();
        m_controls.Player.ClearTime.performed += context => ClearTime();
    }

    private void Update()
    {
        if (m_active)
        {
            m_time += Time.deltaTime;
        }

        TimeSpan t = TimeSpan.FromSeconds(m_time);
        m_stopwatch.text = t.ToString(@"mm\:ss\:fff");
    }

    public void FadeToLevel(int buildIndex)
    {
        if (m_animator)
            m_animator.SetTrigger("FadeOut");
    }

    public void Restart()
    {
        FadeToLevel(SceneManager.GetActiveScene().buildIndex);
    }

    public void ChangeLevel(int buildIndex)
    {
        SceneManager.LoadScene(buildIndex);
    }

    public void Begin()
    {
        if (!m_hasBegun)
        {
            m_active = true;
            m_hasBegun = true;
        }
    }

    public void Finish()
    {
        m_active = false;

        if (m_time < PlayerPrefs.GetFloat("personal_best", float.PositiveInfinity))
        {
            PlayerPrefs.SetFloat("personal_best", m_time);
        }

        m_hasFinished = true;
    }

    public void TryEnd()
    {
        if (m_hasFinished)
        {
            Restart();
        }
    }

    public bool GetHasFinished()
    {
        return m_hasFinished;
    }

    public void ClearTime()
    {
        PlayerPrefs.DeleteKey("personal_best");
        m_personalBest.text = "";
    }
}
