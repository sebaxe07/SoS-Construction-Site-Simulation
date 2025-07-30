using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
    * ModalWindowPanel.cs
    * This script is used to show a modal window.
    * It can be used to show a modal window with a title, image, message, confirm button, decline button, and close button.
    * The modal window can be vertical or horizontal.
    * The modal window can have a title, image, message, confirm button text, decline button text, and close button text.
    * The modal window can have a trigger on enable.
*/

public class ModalWindowPanel : MonoBehaviour
{
    [SerializeField]
    private Transform _modalWindowBox;

    [Header("Header")]
    [SerializeField]
    private Transform _headerArea;
    [SerializeField]
    private TextMeshProUGUI _titleField;

    [Header("Content")]
    [SerializeField]
    private Transform _contentArea;
    [SerializeField]
    private Transform _verticalLayoutArea;
    [SerializeField]
    private Image _heroImage;
    [SerializeField]
    private TextMeshProUGUI _heroText;

    [Space()]
    [SerializeField]
    private Transform _horizontalLayoutArea;
    [SerializeField]
    private Image _iconImage;
    [SerializeField]
    private TextMeshProUGUI _iconText;

    [Header("Footer")]
    [SerializeField]
    private Transform _footerArea;
    [SerializeField]
    private Button _confirmButton;
    [SerializeField]
    private TextMeshProUGUI _confirmButtonText;
    [SerializeField]
    private Button _declineButton;
    [SerializeField]
    private TextMeshProUGUI _declineButtonText;
    [SerializeField]
    private Button _closeButton;
    [SerializeField]
    private TextMeshProUGUI _closeButtonText;

    private Action onConfirm;
    private Action onDecline;
    private Action onClose;

    public void OnConfirmButtonClicked()
    {
        onConfirm?.Invoke();
        Close();
    }

    public void OnDeclineButtonClicked()
    {
        onDecline?.Invoke();
        Close();
    }

    public void OnCloseButtonClicked()
    {
        onClose?.Invoke();
        Close();
    }

    public void SetModal(
        ModalViewMode mode,
        string title,
        Sprite image,
        string message,
        string confirmActionButtonText,
        string declineActionButtonText,
        string closeActionButtonText,
        Action confirmAction,
        Action declineAction = null,
        Action closeAction = null)
    {
        _verticalLayoutArea.gameObject.SetActive(mode == ModalViewMode.Vertical);
        _horizontalLayoutArea.gameObject.SetActive(mode == ModalViewMode.Horizontal);

        _titleField.text = title;

        if (mode == ModalViewMode.Vertical)
        {
            _heroImage.sprite = image;
            _heroText.text = message;

        }
        else if (mode == ModalViewMode.Horizontal)
        {
            _iconImage.sprite = image;
            _iconText.text = message;
        }

        onConfirm = confirmAction;
        onDecline = declineAction;
        onClose = closeAction;
        _confirmButtonText.text = confirmActionButtonText;
        _declineButtonText.text = declineActionButtonText;
        _closeButtonText.text = closeActionButtonText;

        bool hasDeclineAction = declineAction != null;
        _declineButton.gameObject.SetActive(hasDeclineAction);

        bool hasCloseAction = closeAction != null;
        _closeButton.gameObject.SetActive(hasCloseAction);
    }

    public void Show()
    {
        // Call any logic if needed (e.g., animations)
        gameObject.SetActive(true);
    }

    public void Close()
    {
        // Call any cleanup logic if needed (e.g., animations)
        Destroy(gameObject); // Destroys this modal instance
    }
}
