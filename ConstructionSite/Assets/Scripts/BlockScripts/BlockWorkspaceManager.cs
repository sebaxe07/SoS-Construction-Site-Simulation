using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;


public class BlockWorkspaceManager : MonoBehaviour
{
    [Header("Block Prefabs")]
    [SerializeField] private List<BlockPrefabData> blockPrefabs;
    [SerializeField] private SelectedSiteData selectedSiteData;

    [Header("UI References")]
    [SerializeField] private RectTransform paletteArea;
    [SerializeField] private RectTransform buildArea;

    [Header("Grid Settings")]
    [SerializeField] private float horizontalSpacing = 10f;
    [SerializeField] private float verticalSpacing = 10f;
    [SerializeField] private float categorySpacing = 20f;
    [SerializeField] private float blockScale = 0.8f;
    [SerializeField] private int blocksPerRow = 2;

    [Header("Offset Settings")]
    [SerializeField] private float topOffset = 60f;     // Offset from top of palette
    [SerializeField] private float leftOffset = 20f;    // Offset from left side
    [SerializeField] private float rightOffset = 20f;   // Offset from right side

    [Header("Category Settings")]
    [SerializeField] private Color categoryLabelColor = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private int categoryFontSize = 16;
    [SerializeField] private float categoryLabelHeight = 25f;

    private Dictionary<BlockType, GameObject> blockPrefabMap = new Dictionary<BlockType, GameObject>();
    private Vector2 currentPosition;


    // Method for creating a dictionary of block categories and their corresponding block types
    private readonly Dictionary<string, BlockType[]> categories = new Dictionary<string, BlockType[]>
    {
        {
            "Action", new BlockType[]
            {
                BlockType.Move,
                BlockType.MoveZone,
                BlockType.Unload,
                BlockType.Pickup,
                BlockType.Dig,
                BlockType.DropOff,
            }
        },
        {
            "Control", new BlockType[]
            {
                BlockType.TurnOn,
                BlockType.TurnOff,
                BlockType.Idle,
            }
        },
    };

    private void Awake()
    {
        InitializePrefabMap();
    }

    private void Start()
    {
        InitializePalette();

    }

    private void InitializePrefabMap()
    {
        blockPrefabMap = new Dictionary<BlockType, GameObject>();
        foreach (var blockData in blockPrefabs)
        {
            if (blockData != null && blockData.prefab != null)
            {
                blockPrefabMap[blockData.blockType] = blockData.prefab;
            }
        }
    }

    private void InitializePalette()
    {
        // Clear existing palette items
        foreach (Transform child in paletteArea)
        {
            Destroy(child.gameObject);
        }

        // Initialize starting position with top offset
        // In our case -3 * topOffset ensures there is no overlap with the title
        currentPosition = new Vector2(leftOffset, -3 * topOffset);

        float usableWidth = paletteArea.rect.width - leftOffset - rightOffset;
        float blockWidth = (usableWidth - (horizontalSpacing * (blocksPerRow - 1))) / blocksPerRow;

        foreach (var category in categories)
        {
            // Create category label
            CreateCategoryLabel(category.Key);

            // Create blocks for this category
            BlockType[] blocks = category.Value;
            for (int i = 0; i < blocks.Length; i++)
            {
                int row = i / blocksPerRow;
                int col = i % blocksPerRow;

                // Calculate position for this block
                float xPos = leftOffset + (col * (blockWidth + horizontalSpacing));
                float yPos = currentPosition.y - (row * (60f * blockScale + verticalSpacing));

                Vector2 blockPosition = new Vector2(xPos, yPos);
                CreatePaletteBlock(blocks[i], blockPosition, blockWidth);

                // If this is the last block in the category, update the currentPosition
                if (i == blocks.Length - 1)
                {
                    currentPosition.y = yPos - (60f * blockScale + categorySpacing);
                }
            }
        }
    }

    private void CreateCategoryLabel(string categoryName)
    {
        GameObject label = new GameObject(categoryName + "Label");
        label.transform.SetParent(paletteArea, false);

        Text text = label.AddComponent<Text>();
        text.text = categoryName.ToUpper();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = categoryFontSize;
        text.color = categoryLabelColor;
        text.alignment = TextAnchor.MiddleLeft;
        text.fontStyle = FontStyle.Bold;

        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = currentPosition;
        rect.sizeDelta = new Vector2(paletteArea.rect.width - leftOffset - rightOffset, categoryLabelHeight);

        // Update current position for next element
        currentPosition.y -= categoryLabelHeight + verticalSpacing;
    }

    private void CreatePaletteBlock(BlockType blockType, Vector2 position, float width)
    {
        GameObject prefab = GetBlockPrefab(blockType);
        if (prefab != null)
        {
            GameObject block = Instantiate(prefab, paletteArea);
            SetupPaletteBlock(block, position, blockType, width);
        }
    }

    private void SetupPaletteBlock(GameObject block, Vector2 position, BlockType blockType, float width)
    {
        RectTransform rect = block.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one * blockScale;
            rect.sizeDelta = new Vector2(width / blockScale, rect.sizeDelta.y);
        }

        var paletteBlock = block.AddComponent<PaletteBlock>();
        var dropdownManager = block.GetComponent<BlockDropdownManager>();
        dropdownManager?.SetSelectedSiteData(selectedSiteData);
        paletteBlock.Initialize(blockType, this);
    }

    public void SubmitTasks()
    {
        if (SimulationData.Instance == null || string.IsNullOrEmpty(SimulationData.Instance.SelectedZone))
        {
            Debug.LogError("No zone selected or SimulationData instance is missing!");
            return;
        }

        string selectedZone = SimulationData.Instance.SelectedZone;
        Debug.Log($"Submitting tasks for Zone: {selectedZone}");

        TaskManagement.TaskManager.ClearTasksForZone(selectedZone);
        // Collect all blocks in the build area and submit tasks
        foreach (Transform block in buildArea)
        {
            var buildBlock = block.GetComponent<BuildBlock>();
            var dropdownManager = block.GetComponent<BlockDropdownManager>();
            var inputManager = block.GetComponent<BlockInputManager>();


            if (buildBlock == null || dropdownManager == null || inputManager == null)
            {
                Debug.LogError($"Missing components on block: {block.name}");
                continue;
            }

            BlockType blockType = buildBlock.blockType;
            var selections = dropdownManager.GetCurrentSelections();
            string machineType = selections.source;
            string taskType = blockType.ToString();
            string targetZone;

            if (blockType == BlockType.MoveZone)
            {
                targetZone = dropdownManager.GetCurrentSelections().target;
                Debug.Log($"Adding Task for Zone {selectedZone} - Machine: {machineType}, Task: {taskType}, Target Zone: {targetZone}");
                if (!SimulationData.Instance.ZoneMachines.ContainsKey(selectedZone))
                {
                    Debug.LogWarning($"Zone {selectedZone} has no machine positions assigned yet!");
                    continue;
                }

                TaskManagement.TaskManager.AddTask(machineType, taskType, targetZone, selectedZone);
                continue;

            }

            Vector3 position = inputManager.GetCoordinates();



            // Log task details for debugging
            Debug.Log($"Adding Task for Zone {selectedZone} - Machine: {machineType}, Task: {taskType}, Position: {position}");

            // Add the task for the selected zone
            if (!SimulationData.Instance.ZoneMachines.ContainsKey(selectedZone))
            {
                Debug.LogWarning($"Zone {selectedZone} has no machine positions assigned yet!");
                continue;
            }


            // Pass the selectedZone as the fourth parameter
            TaskManagement.TaskManager.AddTask(machineType, taskType, position, selectedZone);
        }

        // Optionally process tasks here or save them for later
        Debug.Log($"Tasks submitted for Zone: {selectedZone}");
    }


    public void UpdateGridLayout(int newBlocksPerRow)
    {
        blocksPerRow = newBlocksPerRow;
        InitializePalette();
    }

    public void UpdateBlockScale(float newScale)
    {
        blockScale = newScale;
        InitializePalette();
    }

    public RectTransform GetBuildArea()
    {
        return buildArea;
    }

    public GameObject GetBlockPrefab(BlockType type)
    {
        if (blockPrefabMap != null)
        {
            if (blockPrefabMap.TryGetValue(type, out GameObject prefab))
            {
                return prefab;
            }
        }
        return null;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (gameObject.activeInHierarchy)
        {
            InitializePalette();
        }
    }
}