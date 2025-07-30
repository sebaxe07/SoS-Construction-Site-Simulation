using UnityEngine;
using UnityEngine.EventSystems;

public class PaletteBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
  private GameObject draggedInstance;
  private Canvas canvas;
  private BlockWorkspaceManager workspaceManager;
  public BlockType blockType { get; private set; }



  public void Initialize(BlockType type, BlockWorkspaceManager workspaceManager)
  {
    blockType = type;
    this.workspaceManager = workspaceManager;
  }

  private void Awake()
  {
    canvas = GetComponentInParent<Canvas>();
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    GameObject prefab = workspaceManager.GetBlockPrefab(blockType);
    if (prefab != null)
    {
      draggedInstance = Instantiate(prefab, canvas.transform);

      Destroy(draggedInstance.GetComponent<PaletteBlock>());
      var buildBlock = draggedInstance.AddComponent<BuildBlock>();
      buildBlock.Initialize(eventData.position, blockType);

      draggedInstance.transform.position = eventData.position;
    }
  }


  public void OnDrag(PointerEventData eventData)
  {
    if (draggedInstance != null)
    {
      draggedInstance.transform.position = eventData.position;
    }
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    if (draggedInstance != null)
    {
      if (RectTransformUtility.RectangleContainsScreenPoint(
          workspaceManager.GetBuildArea(),
          eventData.position))
      {
        draggedInstance.transform.SetParent(workspaceManager.GetBuildArea(), true);
      }
      else
      {
        Destroy(draggedInstance);
      }
    }
  }
}