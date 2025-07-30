using System;
using UnityEngine;

/*
  * ModalManager.cs
  * This script is used to manage the modal windows.
  * It can be used to show a modal window with a title, image, message, confirm button, decline button, and close button.
  * The modal window can be vertical or horizontal.
  * The modal window can have a title, image, message, confirm button text, decline button text, and close button text.
  * The modal window can have a trigger on enable.
*/
public class ModalManager : MonoBehaviour
{
  public static ModalManager Instance { get; private set; }

  [SerializeField] private ModalWindowPanel modalPrefab;
  [SerializeField] private Canvas canvas;
  private void Awake()
  {
    // Singleton logic
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject); // Destroy duplicate instance
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject); // Optional: Keeps it alive across scenes
  }

  /// <summary>
  /// Shows a modal with the specified configuration.
  /// </summary>
  public void ShowModal(
      ModalViewMode mode,
      string title,
      Sprite image,
      string message,
      string confirmText,
      string declineText,
      string closeText,
      Action onConfirm,
      Action onDecline = null,
      Action onClose = null)
  {
    // Instantiate the modal prefab
    ModalWindowPanel modalInstance = Instantiate(modalPrefab, transform);

    // Add the modal to canvas
    modalInstance.transform.SetParent(canvas.transform, false);

    // Configure the modal
    modalInstance.SetModal(mode, title, image, message, confirmText, declineText, closeText, onConfirm, onDecline, onClose);

    // Show the modal
    modalInstance.Show();
  }
}