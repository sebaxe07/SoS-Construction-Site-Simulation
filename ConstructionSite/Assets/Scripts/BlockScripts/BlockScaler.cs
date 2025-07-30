// BlockScaler.cs
using UnityEngine;
using UnityEngine.UI;

public class BlockScaler : MonoBehaviour
{
    [Header("Scaling Settings")]
    [SerializeField] private Vector2 defaultSize = new Vector2(300, 40); // Original size
    [SerializeField] private Vector2 scaledSize = new Vector2(200, 30);  // Scaled size

    private void Start()
    {
        ScaleBlock();
    }

    private void ScaleBlock()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        Image blockImage = GetComponent<Image>();

        if (rectTransform != null && blockImage != null)
        {
            // Set the RectTransform size
            rectTransform.sizeDelta = scaledSize;

            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Scale child elements
            ScaleChildElements(rectTransform, scaledSize.x / defaultSize.x);
        }
    }

    private void ScaleChildElements(RectTransform parent, float scale)
    {
        foreach (RectTransform child in parent)
        {
            // Scale position and size
            child.sizeDelta *= scale;
            child.anchoredPosition *= scale;

            Text text = child.GetComponent<Text>();
            if (text != null)
            {
                text.fontSize = Mathf.RoundToInt(text.fontSize * scale);
            }

            InputField inputField = child.GetComponent<InputField>();
            if (inputField != null)
            {
                Text inputText = inputField.textComponent;
                if (inputText != null)
                {
                    inputText.fontSize = Mathf.RoundToInt(inputText.fontSize * scale);
                }
            }
        }
    }
}