using UnityEngine;
using System.Collections;

// Quits the player when the user hits escape

public class GameMonoManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Debug.Log("QUIT GAME");
            Application.Quit();
        }
    }
}