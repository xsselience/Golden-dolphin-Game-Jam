using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Esc : MonoBehaviour
{
    public GameObject Menu;
    // Start is called before the first frame update
    void Start()
    {
        Menu.SetActive(false);
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
}
