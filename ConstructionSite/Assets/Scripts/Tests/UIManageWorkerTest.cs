using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using constructionSite.Scripts;


public class UIWorkersManagerTests
{
    private GameObject gameObject;
    private UIWorkersManager uiManager;
    private ProjectManager projectManager;
    private GameObject canvas;
    private GameObject scrollView;

    [SetUp]
    public void Setup()
    {
        // Create the AddWorkers parent GameObject
        gameObject = new GameObject("AddWorkers");

        // Create Main Camera
        var cameraObj = new GameObject("Main Camera");
        cameraObj.AddComponent<Camera>();
        cameraObj.transform.SetParent(gameObject.transform);

        // Create Directional Light
        var lightObj = new GameObject("Directional Light");
        lightObj.AddComponent<Light>().type = LightType.Directional;
        lightObj.transform.SetParent(gameObject.transform);

        // Create Canvas with proper setup
        canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.transform.SetParent(gameObject.transform);

        // Create AddWorkerButton
        var addWorkerBtn = CreateButtonWithText("AddWorkerButton", "Add Worker");
        addWorkerBtn.transform.SetParent(canvas.transform, false);

        // Create WorkerPrefab
        var workerPrefab = CreateWorkerPrefab();
        workerPrefab.transform.SetParent(canvas.transform, false);
        workerPrefab.SetActive(false); // Hide the prefab

        // Create ScrollView
        scrollView = CreateScrollView();
        scrollView.transform.SetParent(canvas.transform, false);

        // Create WorkersNumberField
        var numberField = new GameObject("WorkersNumberField");
        CreateInputField(numberField, "WorkersNumberField");
        numberField.transform.SetParent(canvas.transform, false);

        // Create AddWorkersButton
        var addWorkersBtn = CreateButtonWithText("AddWorkersButton", "Add Workers");
        addWorkersBtn.transform.SetParent(canvas.transform, false);

        // Create Header
        var header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(canvas.transform, false);

        // Create EditPopup
        var editPopup = CreatePopupPrefab();
        editPopup.transform.SetParent(canvas.transform, false);
        editPopup.SetActive(false); // Hide the popup by default

        // Create EventSystem
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        eventSystem.transform.SetParent(gameObject.transform);

        // Create ProjectManagerObject
        var pmObject = new GameObject("ProjectManagerObject");
        projectManager = pmObject.AddComponent<ProjectManager>();
        projectManager.Initialize(1, "Test Project");
        pmObject.transform.SetParent(gameObject.transform);

        // Create UIManager
        var uiManagerObj = new GameObject("UIManager");
        uiManager = uiManagerObj.AddComponent<UIWorkersManager>();
        uiManagerObj.transform.SetParent(gameObject.transform);

        // Setup UIManager references
        uiManager.projectManager = projectManager;
        uiManager.addWorkerButton = addWorkerBtn.GetComponent<Button>();
        uiManager.addWorkersButton = addWorkersBtn.GetComponent<Button>();
        uiManager.workerListPanel = scrollView.transform.Find("Viewport/Content").gameObject;
        uiManager.workerPrefab = workerPrefab;
        uiManager.popupPrefab = editPopup;
        uiManager.workersNumberField = numberField;
        uiManager.mainCanvas = canvas;
    }

    private GameObject CreateButtonWithText(string name, string buttonText)
    {
        var buttonObj = new GameObject(name);
        var button = buttonObj.AddComponent<Button>();
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = buttonText;
        return buttonObj;
    }

    private GameObject CreateScrollView()
    {
        // Create ScrollView with all necessary components
        var scrollView = new GameObject("Scroll View", typeof(ScrollRect));
        var scrollRect = scrollView.GetComponent<ScrollRect>();

        // Create Viewport
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollView.transform, false);
        viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);

        // Create Content
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var sizeFitter = content.GetComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);

        // Setup ScrollRect references
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = content.GetComponent<RectTransform>();

        return scrollView;
    }

    private GameObject CreateWorkerPrefab()
    {
        var prefab = new GameObject("WorkerPrefab");

        // Add necessary components (ID, Name, Surname, Role, Status texts and buttons)
        CreateTextComponent(prefab, "WorkerId");
        CreateTextComponent(prefab, "WorkerName");
        CreateTextComponent(prefab, "WorkerSurname");
        CreateTextComponent(prefab, "WorkerRole");
        CreateTextComponent(prefab, "Status");

        // Add buttons
        CreateButton(prefab, "DeleteWorkerButton");
        CreateButton(prefab, "Edit");

        return prefab;
    }

    private GameObject CreatePopupPrefab()
    {
        var prefab = new GameObject("PopupPrefab");
        var canvas = prefab.AddComponent<Canvas>();

        // Add necessary components
        CreateTextComponent(prefab, "WorkerId");
        CreateInputField(prefab, "WorkerName");
        CreateInputField(prefab, "WorkerSurname");
        CreateDropdown(prefab, "WorkerRole");
        CreateTextComponent(prefab, "Status");
        CreateButton(prefab, "SaveButton");
        CreateButton(prefab, "CancelButton");

        return prefab;
    }

    private void CreateTextComponent(GameObject parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.AddComponent<TextMeshProUGUI>();
    }

    private void CreateButton(GameObject parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.AddComponent<Button>();
    }

    private void CreateInputField(GameObject parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        var input = obj.AddComponent<TMP_InputField>();
        var textArea = new GameObject("TextArea");
        textArea.transform.SetParent(obj.transform);
        var inputText = textArea.AddComponent<TextMeshProUGUI>();
        input.textComponent = inputText;
    }

    private void CreateDropdown(GameObject parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.AddComponent<TMP_Dropdown>();
    }

    [TearDown]
    public void Teardown()
    {
        if (gameObject != null)
            Object.DestroyImmediate(gameObject);
    }

    [UnityTest]
    public IEnumerator AddWorker_CreatesNewWorkerInList()
    {
        // Arrange
        int initialWorkerCount = projectManager.Workers.Count;
        HelperWorkers.isTest = true; // Prevent overwriting the JSON file
        // Act
        uiManager.addWorkerButton.onClick.Invoke();
        yield return new WaitForEndOfFrame();
        // Wait one more frame for ScrollView to update
        yield return new WaitForEndOfFrame();

        // Assert
        Assert.AreEqual(initialWorkerCount + 1, projectManager.Workers.Count, "Worker was not added to ProjectManager");
        Assert.AreEqual(initialWorkerCount + 1, uiManager.workerListPanel.transform.childCount, "Worker UI element was not created");
    }

    [UnityTest]
    public IEnumerator DeleteWorker_RemovesWorkerFromList()
    {
        // Arrange
        // Add three workers

        HelperWorkers.isTest = true; // Prevent overwriting the JSON file
        uiManager.addWorkerButton.onClick.Invoke();
        yield return new WaitForEndOfFrame();

        HelperWorkers.isTest = true; // Prevent overwriting the JSON file
        uiManager.addWorkerButton.onClick.Invoke();
        yield return new WaitForEndOfFrame();

        HelperWorkers.isTest = true; // Prevent overwriting the JSON file
        uiManager.addWorkerButton.onClick.Invoke();
        yield return new WaitForEndOfFrame();

        int initialWorkerCount = projectManager.Workers.Count;

        // Find the delete button specifically by looking for the DeleteWorkerButton GameObject
        var workerUI = uiManager.workerListPanel.transform.GetChild(0); // Get first worker in list
        var deleteButton = workerUI.Find("DeleteWorkerButton")?.GetComponent<Button>();

        Assert.IsNotNull(deleteButton, "Delete button not found on worker UI element");

        // Act
        HelperWorkers.isTest = true; // Prevent overwriting the JSON file
        deleteButton.onClick.Invoke();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Wait an extra frame for UI updates

        // Assert
        Assert.AreEqual(initialWorkerCount - 1, projectManager.Workers.Count,
            "Worker count in ProjectManager did not decrease");
        Assert.AreEqual(initialWorkerCount - 1, uiManager.workerListPanel.transform.childCount,
            "Worker UI element count did not decrease");

        // Additional verification
        var remainingWorkers = projectManager.Workers;
        var remainingUIElements = uiManager.workerListPanel.transform.childCount;
        Debug.Log($"Remaining workers in ProjectManager: {remainingWorkers.Count}");
        Debug.Log($"Remaining UI elements: {remainingUIElements}");
    }
}
