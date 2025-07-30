using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [Header("Loading Screen Settings")]
    public GameObject loadingScreen;
    public Slider progressBar;
    public TextMeshProUGUI progressText;

    // Method to load a scene normally (without loading screen)
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadSceneAsync(sceneIndex);
    }

    public void LoadAdditiveScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncLoad.completed += (AsyncOperation operation) =>
        {
            // Find in the new scene the button with the name "Back" and assign the method to unload that scene
            Scene newScene = SceneManager.GetSceneByName(sceneName);
            if (newScene.IsValid())
            {
                GameObject[] rootObjects = newScene.GetRootGameObjects();
                foreach (GameObject rootObject in rootObjects)
                {
                    // Find the event system in the new scene and delete it
                    if (rootObject.GetComponent<EventSystem>() != null)
                    {
                        Debug.LogError("Event System found");
                        Destroy(rootObject);
                        continue;
                    }

                    if (rootObject.GetComponent<AudioListener>() != null)
                    {
                        Debug.LogError("Audio Listener found");
                        Destroy(rootObject);
                        continue;
                    }


                    // Find the button with the name "Back" and assign the method to unload that scene
                    Button backButton = FindButtonInHierarchy(rootObject);
                    if (backButton != null)
                    {
                        Debug.LogError("Button found");
                        backButton.onClick.AddListener(() => UnloadScene(newScene.name));
                    }

                }
            }
            else
            {
                Debug.LogError("Scene not found");
            }
        };
    }

    private Button FindButtonInHierarchy(GameObject parent)
    {
        // Check if the current GameObject has a Button component
        Button button = parent.GetComponent<Button>();
        if (button != null && button.name == "Back")
        {
            return button;
        }

        // Recursively search children
        foreach (Transform child in parent.transform)
        {
            Button foundButton = FindButtonInHierarchy(child.gameObject);
            if (foundButton != null)
            {
                return foundButton;
            }
        }

        return null; // No button found in this hierarchy
    }

    public void UnloadScene(string sceneName)
    {
        SceneManager.UnloadSceneAsync(sceneName);
    }

    // Method to load a scene asynchronously with a loading screen
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // Activate the loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Start loading the scene asynchronously
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;

        // Custom loading steps
        string[] loadingSteps =
        {
        "Starting Site",
        "Loading Site",
        "Reading Site Data",
        "Building Site Zones",
        "Building Objects in Zones",
        "Building Navigation Mesh"
    };

        // Corresponding progress values for each step
        float[] progressSteps = { 0.1f, 0.2f, 0.5f, 0.6f, 0.7f, 0.9f };

        // Loop through each custom loading step
        for (int i = 0; i < loadingSteps.Length; i++)
        {
            // Update the loading text
            if (progressText != null)
            {
                progressText.text = loadingSteps[i];
            }

            // Gradually increase the progress bar up to the current step's value
            float targetProgress = progressSteps[i];
            while (progressBar != null && progressBar.value < targetProgress)
            {
                progressBar.value = Mathf.MoveTowards(progressBar.value, targetProgress, Time.deltaTime * 0.5f); // Smooth transition
                yield return null;
            }

            yield return new WaitForSeconds(0.5f); // Optional delay between steps
        }

        // Now, wait for the scene to actually load (while staying at 90% progress)
        if (progressText != null)
        {
            progressText.text = "Finalizing...";
        }

        while (!asyncOperation.isDone)
        {
            // Keep the progress at 90% until the scene is ready
            if (progressBar != null)
            {
                progressBar.value = 0.9f;
            }

            // Check if the scene has finished loading
            if (asyncOperation.progress >= 0.9f)
            {
                // Small delay to let the player see "Finalizing..." message
                yield return new WaitForSeconds(0.5f);

                // Activate the scene
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }

        // Deactivate the loading screen after loading is complete
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }


    // Method to quit the application
    public void QuitApplication()
    {
        Application.Quit();
    }
}
