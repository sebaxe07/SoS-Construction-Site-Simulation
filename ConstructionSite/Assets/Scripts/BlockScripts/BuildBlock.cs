using UnityEngine;
using UnityEngine.EventSystems;

public class BuildBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
  private RectTransform rectTransform;
  private Canvas canvas;
  private Vector2 originalPosition;
  private Transform originalParent;
  public BlockType blockType { get; private set; }

  private void Awake()
  {
    rectTransform = GetComponent<RectTransform>();
    canvas = GetComponentInParent<Canvas>();
  }

  public void Initialize(Vector2 position, BlockType type)
  {
    rectTransform.position = position;
    blockType = type;
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    originalPosition = rectTransform.anchoredPosition;
    originalParent = transform.parent;
    transform.SetAsLastSibling();
  }

  public void OnDrag(PointerEventData eventData)
  {
    rectTransform.position = eventData.position;
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    var workspaceManager = GetComponentInParent<BlockWorkspaceManager>();
    if (!RectTransformUtility.RectangleContainsScreenPoint(
        workspaceManager.GetBuildArea(),
        eventData.position))
    {
      Destroy(gameObject);
    }
  }
}