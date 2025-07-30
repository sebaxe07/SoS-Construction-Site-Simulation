using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Trigger.cs
 * This script is used to trigger a modal window when the object is enabled.
 * It can be used to show a modal window when the object is enabled.
 * The modal window can be vertical or horizontal.
 * The modal window can have a title, image, message, confirm button text, decline button text, and close button text.
 * The modal window can have a trigger on enable.
 * The modal window can have a modal manager.
 * THE SCRIPT IS A DEMO SCRIPT FOR HOW TO TRIGGER A MODAL WINDOW.
 */
public class Trigger : MonoBehaviour
{
    public bool isVertical;
    public string title;
    public Sprite image;
    public string message;
    public string confirmButtonText;
    public string declineButtonText;
    public string closeButtonText;

    public void OnEnable()
    {
        ModalManager.Instance?.ShowModal(
            isVertical ? ModalViewMode.Vertical : ModalViewMode.Horizontal,
            title, image, message, confirmButtonText, declineButtonText, closeButtonText, null, null, null);
    }

}
