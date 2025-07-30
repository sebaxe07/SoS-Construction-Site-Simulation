#region imports
using System.Threading.Tasks;
using System.Collections; // Per IEnumerator
using System.Collections.Generic; // Per IEnumerator
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#endregion

#region JSON Parser class
/// <summary>
/// Represents the structure of the payload received from the MQTT broker.
/// </summary>
[System.Serializable]
public class Payload
{
    public float[] coord;  // Array di coordinate
    public string id;      // ID of the machine
    public string task;    // Type of task
    public long timestamp; // Timestamp
}
#endregion

/// <summary>
/// Handles MQTT communication and updates the Unity UI based on messages received.
/// </summary>
public class MQTTHandler : MonoBehaviour
{
    #region UI Elements
    [Header("UI Elements")]

    [Header("Texts")]
    public TMP_Text connectionStatusText;
    public TMP_Text subscriptionStatusText;
    public TMP_Text messagesText;
    public TMP_Text excavatorStatusText;
    public TMP_Text truckStatusText;
    public TMP_Text wheelloaderStatusText;

    [Header("Input fields")]
    public TMP_InputField subTopicInput;

    [Header("Panels")]
    public RectTransform mapPanel;
    public RectTransform excavatorPanel, truckPanel, wheelloaderPanel;
    #endregion

    # region Constants
    const float UPDATE_RATE = 0.1f;
    #endregion

    #region Private Variable
    private float mapWidth;
    private float mapHeight;

    private MQTTManager mqttManager;
    private Payload[] previousLastMsg = new Payload[3];
    private Payload[] lastMsg = new Payload[3];

    private bool isSubscribed = false;
    private bool isConnected = false;
    private string topic = "";
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        // Initialize UI status
        connectionStatusText.text = "Not Connected";
        connectionStatusText.color = Color.red;
        subscriptionStatusText.text = "Not Subscribed";
        subscriptionStatusText.color = Color.red;
        messagesText.text = "";

        // Initialize map dimensions
        if (mapPanel != null)
        {
            mapWidth = mapPanel.rect.width;
            mapHeight = mapPanel.rect.height;
        }

        // Start periodic UI updates
        StartCoroutine(UpdateUIRoutine());
        // Initialize MQTT Manager
        mqttManager = new MQTTManager();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Connects or disconnects from the MQTT broker based on the current connection status.
    /// </summary>
    public async void ConnectToBroker()
    {
        if (!isConnected)
        {
            // Attempt to connect
            isConnected = await mqttManager.ConnectToBroker();
            UpdateConnectionStatus();       // Update UI
        }
        else
        {
            // Disconnect if already connected
            isConnected = false;
            UpdateConnectionStatus();       // Update UI
            DisconnectClient();             // Disconnect from broker
        }
    }

    /// <summary>
    /// Subscribes or unsubscribes from the specified topic based on the current subscription status.
    /// </summary>
    public async void SubscribeToTopic()
    {
        topic = subTopicInput.text;

        if (!isSubscribed)
        {
            // Subscribe to topic
            isSubscribed = await mqttManager.SubscribeToTopic(topic, HandleMessageReceived);
            UpdateSubscriptionStatus();     // Update UI
        }
        else
        {
            // Unsubscribe from topic
            isSubscribed = false;
            UpdateSubscriptionStatus();     // Update UI
            UnsubscribeClient();            // Unsubscribe from topic
        }
    }

    #endregion

    #region Private Methods
    /// <summary>Updates the connection status UI based on the current connection state.</summary>
    private void UpdateConnectionStatus()
    {
        connectionStatusText.text = isConnected ? "Connected" : "Not Connected";
        connectionStatusText.color = isConnected ? Color.green : Color.red;
    }

    /// <summary>Updates the subscription status UI based on the current subscription state.</summary>
    private void UpdateSubscriptionStatus()
    {
        subscriptionStatusText.text = isSubscribed ? "Subscribed" : "Not Subscribed";
        subscriptionStatusText.color = isSubscribed ? Color.green : Color.red;
    }

    /// <summary>Handles incoming MQTT messages and updates the payload data.</summary>
    /// <param name="topic">Topic of the received message.</param>
    /// <param name="message">Message payload in JSON format.</param>
    private void HandleMessageReceived(string topic, string message)
    {
        // Parse JSON payload
        Payload data = JsonUtility.FromJson<Payload>(message);

        // Assign data to corresponding machine based on its ID
        switch (data.id)
        {
            case "M001":
                lastMsg[0] = data;
                break;
            case "M002":
                lastMsg[1] = data;
                break;
            case "M004":
                lastMsg[2] = data;
                break;
            default:
                Debug.LogWarning($"Unknown machine type: {data.id}");
                return;
        }
    }

    /// <summary>Periodically updates the UI to reflect the latest payload data.</summary>
    private IEnumerator UpdateUIRoutine()
    {
        while (true)
        {
            UpdateUI();                                     // Update machines position and log in UI
            yield return new WaitForSeconds(UPDATE_RATE);   // Wait for 10 seconds
        }
    }

    /// <summary>Updates the UI with the latest payload data, including machine positions and messages.</summary>
    private void UpdateUI()
    {
        for (int i = 0; i < lastMsg.Length; i++)
        {
            if (lastMsg[i] != null)
            {
                // Format current timestamp for display in hh:mm:ss
                TimeSpan timeSpan = TimeSpan.FromMilliseconds(lastMsg[i].timestamp);
                string formattedTimestamp = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours % 24, timeSpan.Minutes, timeSpan.Seconds);

                if (lastMsg[i] != previousLastMsg[i])
                {

                    // Normalize coordinates to fit the map
                    Vector2 normalizedPosition = new Vector2(
                        (lastMsg[i].coord[0]) / 11,
                        (lastMsg[i].coord[1]) / 11
                    );

                    // Map normalized position to UI dimensions
                    Vector2 mappedPosition = new Vector2(
                        normalizedPosition.x * mapWidth,
                        normalizedPosition.y * mapHeight
                    );

                    RectTransform targetPanel = null;

                    // Determine which panel to update
                    switch (lastMsg[i].id)
                    {
                        case "M001":
                            targetPanel = excavatorPanel;
                            excavatorStatusText.text = "Excavator started " + lastMsg[i].task + " at " + formattedTimestamp;
                            break;
                        case "M002":
                            targetPanel = truckPanel;
                            truckStatusText.text = "Truck started " + lastMsg[i].task + " at " + formattedTimestamp;
                            break;
                        case "M004":
                            targetPanel = wheelloaderPanel;
                            wheelloaderStatusText.text = "Wheel loader started " + lastMsg[i].task + " at " + formattedTimestamp;
                            break;
                        default:
                            Debug.LogWarning($"Unknown machine type: {lastMsg[i].id}");
                            return;
                    }

                    // Update panel position
                    if (targetPanel != null)
                    {
                        targetPanel.anchoredPosition = mappedPosition;
                    }

                    previousLastMsg[i] = lastMsg[i];

                    // Create and display new log message
                    string newMessage = formattedTimestamp + " - " + lastMsg[i].id + " -> " + lastMsg[i].task + " at pos " + lastMsg[i].coord[0].ToString("F1") + ", " + lastMsg[i].coord[1].ToString("F1") + "\n";
                    messagesText.text = newMessage + messagesText.text;
                }
                else if (lastMsg[i] == previousLastMsg[i])
                {
                    // Format task time for display in hh:mm:ss
                    long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    TimeSpan timeSpanDiff = TimeSpan.FromMilliseconds(currentTime - previousLastMsg[i].timestamp);
                    string formattedTaskTime = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpanDiff.Hours, timeSpanDiff.Minutes, timeSpanDiff.Seconds);

                    switch (lastMsg[i].id)
                    {
                        case "M001":
                            excavatorStatusText.text = "Excavator has " + lastMsg[i].task + " for " + formattedTaskTime + " and started at " + formattedTimestamp;
                            break;
                        case "M002":
                            truckStatusText.text = "Truck has " + lastMsg[i].task + " for " + formattedTaskTime + " and started at " + formattedTimestamp;
                            break;
                        case "M004":
                            wheelloaderStatusText.text = "Wheel loader has " + lastMsg[i].task + " for " + formattedTaskTime + " and started at " + formattedTimestamp;
                            break;
                        default:
                            Debug.LogWarning($"Unknown machine type: {lastMsg[i].id}");
                            return;
                    }
                }
            }
        }
    }

    /// <summary>Disconnects the client from the MQTT broker.</summary>
    private async void DisconnectClient()
    {
        await mqttManager.Disconnect();
    }

    /// <summary>Unsubscribes the client from the current topic.</summary>
    private async void UnsubscribeClient()
    {
        await mqttManager.UnsubscribeFromTopics(topic);
    }

    #endregion
}
