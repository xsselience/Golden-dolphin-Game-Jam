using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("玩家预制体")]
    [SerializeField] private GameObject playerPrefab;

    [Header("死亡 UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private string mainMenuScene = "MainMenu";

    private player currentPlayer;
    private GameObject playerObj;

    private bool spawningInProgress = false;
    private bool isLoadingSave = false;
    private SaveData pendingSaveData;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(SpawnPlayerDelayed(scene));
    }

    IEnumerator SpawnPlayerDelayed(Scene scene)
    {
        if (spawningInProgress) yield break;
        spawningInProgress = true;

        yield return null;

        Vector3 spawnPos = Vector3.zero;
        GameObject spawnObj = GameObject.Find("PlayerSpawn");
        if (spawnObj != null) spawnPos = spawnObj.transform.position;

        if (playerObj == null || playerObj.scene != scene)
        {
            GameObject existing = GameObject.FindGameObjectWithTag("Player");
            if (existing != null)
            {
                playerObj = existing;
                currentPlayer = existing.GetComponent<player>();
            }
            else if (playerPrefab != null)
            {
                playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                currentPlayer = playerObj.GetComponent<player>();
            }

            if (currentPlayer != null)
            {
                DontDestroyOnLoad(playerObj);
            }
        }

        if (currentPlayer == null)
        {
            spawningInProgress = false;
            yield break;
        }

        Rigidbody2D rb = currentPlayer.GetComponent<Rigidbody2D>();
        RigidbodyType2D originalType = RigidbodyType2D.Kinematic;
        if (rb != null)
        {
            originalType = rb.bodyType;
            rb.bodyType = RigidbodyType2D.Kinematic;  // 冻结物理
        }

        if (isLoadingSave && pendingSaveData != null)
        {
            Vector3 savedPos = new Vector3(pendingSaveData.playerPosX, pendingSaveData.playerPosY, pendingSaveData.playerPosZ);
            currentPlayer.transform.position = savedPos;
            currentPlayer.health = pendingSaveData.playerHealth;
            currentPlayer.SetHackCount(pendingSaveData.hackCount);
            currentPlayer.SetCyberPower(pendingSaveData.cyberPower);
            currentPlayer.SetCyberEnabled(pendingSaveData.cyberSystemEnabled);
            Debug.Log($"读档完成，玩家位置: {currentPlayer.transform.position}");
        }
        else
        {
            currentPlayer.transform.position = spawnPos;
            Debug.Log($"正常生成，玩家位置: {spawnPos}");
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = originalType;  // 恢复物理
        }

        isLoadingSave = false;
        pendingSaveData = null;
        spawningInProgress = false;
    }

    public void LoadFromSlot(int slot)
    {
        SaveData data = SaveManager.Load(slot);
        if (data == null) return;

        isLoadingSave = true;
        pendingSaveData = data;

        if (SceneManager.GetActiveScene().name != data.sceneName)
            SceneManager.LoadScene(data.sceneName);
        else
            StartCoroutine(SpawnPlayerDelayed(SceneManager.GetActiveScene()));
    }

    public void SaveToSlot(int slot)
    {
        if (currentPlayer == null) return;

        SaveData data = new SaveData();
        Vector3 pos = currentPlayer.transform.position;
        data.playerPosX = pos.x;
        data.playerPosY = pos.y;
        data.playerPosZ = pos.z;
        data.playerHealth = currentPlayer.health;
        data.hackCount = currentPlayer.GetHackCount();
        data.cyberPower = currentPlayer.GetCyberPower();
        data.cyberSystemEnabled = currentPlayer.IsCyberEnabled();
        data.sceneName = SceneManager.GetActiveScene().name;

        boss1ai b1 = FindObjectOfType<boss1ai>();
        data.boss1Dead = (b1 == null || !b1.enabled);
        boss2ai b2 = FindObjectOfType<boss2ai>();
        data.boss2Dead = (b2 == null || !b2.enabled);

        SaveManager.Save(slot, data);
        Debug.Log($"存档到槽位 {slot} 完成");
    }

    public void DeleteSlot(int slot)
    {
        SaveManager.Delete(slot);
    }

    public void LoadLatestSave()
    {
        int latest = SaveManager.GetLatestSlot();
        if (latest >= 0)
            LoadFromSlot(latest);
        else
            SceneManager.LoadScene(mainMenuScene);
    }

    public void OnPlayerDied()
    {
        if (currentPlayer != null)
            currentPlayer.controlsDisabled = true;
        if (deathPanel != null)
            deathPanel.SetActive(true);
    }

    public void Retry()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
        LoadLatestSave();
    }

    public void BackToMenu()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
        SceneManager.LoadScene(mainMenuScene);
    }

    public void SwitchScene(string sceneName)
    {
        SaveToSlot(-1);
        SceneManager.LoadScene(sceneName);
    }
}
