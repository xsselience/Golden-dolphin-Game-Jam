using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // ─── 按钮调用这个 ───
    public void OnStartGameButton()
    {
        anim.SetBool("gogame", true);
    }

    // ─── 动画事件帧调用这个 ───
    public void LoadNextScene()
    {
        SceneManager.LoadScene(1);
        // 或者按索引：SceneManager.LoadScene(1);
    }
}
