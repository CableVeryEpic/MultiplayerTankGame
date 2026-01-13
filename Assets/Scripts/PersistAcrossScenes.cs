using UnityEngine;

public class PersistAcrossScenes : MonoBehaviour
{

    private static PersistAcrossScenes instance;
    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
