using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Esc : MonoBehaviour
{
    public GameObject Menu;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Openmenu()//做个点击吧老黄划给ui了
    {
            Menu.SetActive(true);
    }

    public void Closemenu()
    {
        Menu.SetActive(false);
    }

    public void backmuen()
    {
        SceneManager.LoadScene(0);
    }
}
