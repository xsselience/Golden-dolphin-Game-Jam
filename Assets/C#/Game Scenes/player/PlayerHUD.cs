using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;


public class PlayerHUD : MonoBehaviour
{
    [Header("血量")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Text healthText;

    [Header("算力")]
    [SerializeField] private GameObject cyberBarGroup;
    [SerializeField] private Image cyberFill;
    [SerializeField] private Text cyberText;
    [SerializeField] private float maxCyberDisplay = 100f;

    [Header("设置")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject mainPanel;       // 主面板
    [SerializeField] private GameObject savePanel;        // 存档子面板
    [SerializeField] private GameObject volumePanel;      // 音量子面板

    [Header("存档")]
    [SerializeField] private Text saveSlot0Text;
    [SerializeField] private Text saveSlot1Text;
    [SerializeField] private Text saveSlot2Text;

    [Header("音量")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioMixer audioMixer;

    private player playerScript;

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (SceneManager.GetActiveScene().buildIndex == 0)
            gameObject.SetActive(false);

        playerScript = GetComponentInParent<player>();

        if (cyberBarGroup != null) cyberBarGroup.SetActive(false);
        CloseAllPanels();

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenMain);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameObject.SetActive(scene.buildIndex != 0);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (playerScript == null) return;

        // 血量
        if (healthFill != null)
            healthFill.fillAmount = (float)playerScript.health / 100f;
        if (healthText != null)
            healthText.text = $"HP: {playerScript.health}";

        // 算力
        if (playerScript.IsCyberEnabled() && cyberBarGroup != null && !cyberBarGroup.activeSelf)
            cyberBarGroup.SetActive(true);
        if (cyberBarGroup != null && cyberBarGroup.activeSelf)
        {
            if (cyberFill != null)
                cyberFill.fillAmount = (float)playerScript.GetCyberPower() / maxCyberDisplay;
            if (cyberText != null)
                cyberText.text = $"算力: {playerScript.GetCyberPower()}";
        }

        // ESC 关闭
        if (mainPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            if (savePanel.activeSelf) BackToMain();
            else if (volumePanel.activeSelf) BackToMain();
            else CloseAllPanels();
        }
    }

    // ==================== 面板开关 ====================

    void OpenMain()
    {
        Time.timeScale = 0f;
        if (playerScript != null) playerScript.controlsDisabled = true;
        mainPanel.SetActive(true);
        savePanel.SetActive(false);
        volumePanel.SetActive(false);
    }

    void CloseAllPanels()
    {
        Time.timeScale = 1f;
        if (playerScript != null) playerScript.controlsDisabled = false;
        mainPanel.SetActive(false);
        savePanel.SetActive(false);
        volumePanel.SetActive(false);
    }

    // ==================== 主面板按钮 ====================

    public void ResumeGame() => CloseAllPanels();

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void OpenSave()
    {
        mainPanel.SetActive(false);
        savePanel.SetActive(true);
        RefreshSaveSlots();
    }

    public void OpenVolume()
    {
        mainPanel.SetActive(false);
        volumePanel.SetActive(true);

        if (volumeSlider != null && audioMixer != null)
        {
            float vol;
            audioMixer.GetFloat("MasterVolume", out vol);
            volumeSlider.value = vol;
        }
    }

    public void BackToMain()
    {
        savePanel.SetActive(false);
        volumePanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    // ==================== 存档 ====================

    public void SaveToSlot0() => SaveAndRefresh(0);
    public void SaveToSlot1() => SaveAndRefresh(1);
    public void SaveToSlot2() => SaveAndRefresh(2);

    void SaveAndRefresh(int slot)
    {
        GameManager.Instance?.SaveToSlot(slot);
        RefreshSaveSlots();
    }

    void RefreshSaveSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            SaveData d = SaveManager.Load(i);
            string text = d != null && !d.isEmpty
                ? $"存档{i + 1}: 已存档"
                : $"存档{i + 1}: 空";

            if (i == 0 && saveSlot0Text != null) saveSlot0Text.text = text;
            if (i == 1 && saveSlot1Text != null) saveSlot1Text.text = text;
            if (i == 2 && saveSlot2Text != null) saveSlot2Text.text = text;
        }
    }

    // ==================== 音量 ====================

    public void OnVolumeChanged()
    {
        if (volumeSlider != null && audioMixer != null)
            audioMixer.SetFloat("MasterVolume", volumeSlider.value);
    }

    // ==================== 读档 ====================
    public void LoadLatestAutoSave()
    {
        Time.timeScale = 1f;
        int latest = SaveManager.GetLatestSlot();
        if (latest >= 0)
            GameManager.Instance?.LoadFromSlot(latest);
        else
            SceneManager.LoadScene(0);
    }
}
