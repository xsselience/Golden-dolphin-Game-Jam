using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class PlayerHUD : MonoBehaviour
{
    [Header("血量")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Text healthText;

    [Header("菜单")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button backToMenuButton;

    [Header("算力")]
    [SerializeField] private GameObject cyberBarGroup;      // 算力条整体（初始隐藏）
    [SerializeField] private Image cyberFill;               // Image Filled 类型
    [SerializeField] private Text cyberText;
    [SerializeField] private float maxCyberDisplay = 100f;

    private player playerScript;
    private bool isPaused = false;

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (SceneManager.GetActiveScene().buildIndex == 0)
            gameObject.SetActive(false);

        playerScript = GetComponentInParent<player>();

        if (menuPanel != null) menuPanel.SetActive(false);

        if (menuButton != null) menuButton.onClick.AddListener(OpenMenu);
        if (resumeButton != null) resumeButton.onClick.AddListener(CloseMenu);
        if (saveButton != null) saveButton.onClick.AddListener(() =>
        {
            GameManager.Instance?.SaveToSlot(0);
            CloseMenu();
        });
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        });
        if (cyberBarGroup != null) cyberBarGroup.SetActive(false);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
            gameObject.SetActive(false);
        else
            gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (playerScript == null) return;

        if (healthFill != null)
            healthFill.fillAmount = (float)playerScript.health / 100f;

        if (healthText != null)
            healthText.text = $"HP: {playerScript.health}";

        if (playerScript != null)
        {
            if (playerScript.IsCyberEnabled() && cyberBarGroup != null && !cyberBarGroup.activeSelf)
                cyberBarGroup.SetActive(true);

            if (cyberBarGroup != null && cyberBarGroup.activeSelf)
            {
                if (cyberFill != null)
                    cyberFill.fillAmount = (float)playerScript.GetCyberPower() / maxCyberDisplay;
                if (cyberText != null)
                    cyberText.text = $"算力: {playerScript.GetCyberPower()}";
            }
        }
    }

    void OpenMenu()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (menuPanel != null) menuPanel.SetActive(true);
        if (playerScript != null) playerScript.controlsDisabled = true;
    }

    void CloseMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (playerScript != null) playerScript.controlsDisabled = false;
    }
}
