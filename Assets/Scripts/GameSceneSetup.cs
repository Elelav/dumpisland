using UnityEngine;

public class GameSceneSetup : MonoBehaviour
{
    void Start()
    {        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameMusic();
        }
    }
}