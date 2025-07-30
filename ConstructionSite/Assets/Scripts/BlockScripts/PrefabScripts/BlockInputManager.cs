using UnityEngine;
using TMPro;

public class BlockInputManager : MonoBehaviour
{
    [Header("Coordinate Input Fields")]
    [SerializeField] private TMP_InputField xCoordinate;
    [SerializeField] private TMP_InputField yCoordinate;

    [Header("Block Type")]
    [SerializeField] private BlockType blockType;

    private void Start()
    {
        InitializeInputFields();
    }

    private void InitializeInputFields()
    {
        if (xCoordinate != null && yCoordinate != null)
        {
            xCoordinate.contentType = TMP_InputField.ContentType.DecimalNumber;
            yCoordinate.contentType = TMP_InputField.ContentType.DecimalNumber;

            // Input validation
            xCoordinate.onValueChanged.AddListener(ValidateCoordinate);
            yCoordinate.onValueChanged.AddListener(ValidateCoordinate);
        }
    }

    // To be implemented later: Input validation
    private void ValidateCoordinate(string value)
    {
        if (float.TryParse(value, out float coordinate))
        {
            // I will add input Validation here later
            // For example, limit range, check for valid positions, etc.
        }
    }

    public Vector3 GetCoordinates()
    {
        float x = float.TryParse(xCoordinate.text, out float xValue) ? xValue : 0f;
        float z = float.TryParse(yCoordinate.text, out float yValue) ? yValue : 0f;
        return new Vector3(x, 0, z);
    }
}