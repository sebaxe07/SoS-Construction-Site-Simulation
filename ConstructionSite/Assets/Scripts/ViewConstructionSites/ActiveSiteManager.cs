using UnityEngine;

public class ActiveSiteManager : MonoBehaviour
{
    public static ActiveSiteManager Instance { get; private set; }

    public ConstructionSiteLoader CurrentSite { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
