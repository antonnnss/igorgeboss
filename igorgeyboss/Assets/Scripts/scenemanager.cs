using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class knopka : MonoBehaviour
{

    public void Transtion()
    {
        SceneManager.LoadScene("SampleScene");
    }
    public void MainMenu()
    {
        SceneManager.LoadScene("menu");
    }

    public void Vihod()
    {
        Application.Quit();
    }

}