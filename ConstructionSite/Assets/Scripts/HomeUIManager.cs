using UnityEngine;

namespace constructionSite.Scripts
{
    public class HomeUIManager : MonoBehaviour
    {
        public ProjectManager projectManager;

        public void Start()
        {
            if (ProjectManager.Instance == null)
            {
                Debug.Log("Creating instance of ProjectManager");
                projectManager.Initialize(1, "Construction Project");
            }
        }
    }

}