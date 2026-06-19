using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    public GameObject Settings;

    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
}
