using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/*
  * BlockSubmitAlert.cs
  * This script is used to trigger a modal window when the object is enabled.
  * It can be used to show a modal window when the object is enabled.
  * The modal window can be vertical or horizontal.
  * The modal window can have a title, image, message, confirm button text, decline button text, and close button text.
  * The modal window can have a trigger on enable.
  * The modal window can have a modal manager.
  * THE SCRIPT IS A DEMO SCRIPT FOR HOW TO TRIGGER A MODAL WINDOW.
*/
public class BlockSubmitAlert : MonoBehaviour
{

  [SerializeField]
  private Button _confirmButton;
  public void OnClickButton()
  {
    Debug.Log("Clicked");

    Action action1 = () =>
    {
      Debug.Log("1 Clicked");
    };
    Action action2 = () =>
    {
      Debug.Log("2 Clicked");
    };
    Action action3 = () =>
    {
      Debug.Log("3 Clicked");
    };

    Sprite warningImage = Resources.Load<Sprite>("Images/Template Images/warning");
    ModalManager.Instance.ShowModal(ModalViewMode.Vertical, "Notification", warningImage, "This will cancel the complex task generation and you will not go to the Simulation Mode.", "Ok", "No", "Close Me", action1, action2, action3);
  }
}