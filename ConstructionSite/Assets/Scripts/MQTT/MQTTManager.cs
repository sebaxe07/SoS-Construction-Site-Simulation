#region imports
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet;
using MQTTnet.Client;
using UnityEngine;
#endregion

/// <summary>
/// Manages MQTT connections, subscriptions, publishing, and disconnections.
/// </summary>
public class MQTTManager
{
    #region Constants
    private const string BROKER_IP = "jca8ae90.ala.eu-central-1.emqxsl.com";    // Broker IP address
    private const int BROKER_PORT = 8883;                                       // Broker port for secure connection
    private const string USERNAME = "DSD-Site-Gateway";                         // MQTT auth username
    private const string PASSWORD = "DSD30L!";                                  // MQTT auth password
    #endregion

    #region Fields
    private IMqttClient mqttClient;                                             // MQTT client instance
    private List<X509Certificate> certs = new List<X509Certificate>
    {
        new X509Certificate2("Assets/StreamingAssets/emqxsl-ca.crt")            // CA Certificate for secure connection
    };
    #endregion

    #region Connect to Broker
    /// <summary> Establishes a connection to the MQTT broker. </summary>
    /// <returns> True if the connection is successful; otherwise, false.</returns>
    public async Task<bool> ConnectToBroker()
    {
        try
        {
            // Create a new MQTT client factory and client instance
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            // Build MQTT client options with TLS and credentials
            var mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(BROKER_IP, BROKER_PORT)
                .WithCredentials(USERNAME, PASSWORD)
                .WithCleanSession()
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    Certificates = certs
                }
                )
                .Build();

            // Attempt to connect to the MQTT broker
            await mqttClient.ConnectAsync(mqttOptions);
            return mqttClient.IsConnected;
        }
        catch (Exception ex)
        {
            // Log any connection errors
            Debug.LogError($"Error connecting to MQTT broker: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Subscription Methods
    /// <summary>Subscribes to a specified topic and handles incoming messages.</summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="onMessageReceived">Callback to handle received messages.</param>
    /// <returns>True if the subscription is successful; otherwise, false.</returns>
    public async Task<bool> SubscribeToTopic(string topic, Action<string, string> onMessageReceived)
    {
        // Check if the MQTT client is connected
        if (mqttClient == null || !mqttClient.IsConnected)
        {
            Debug.LogError("Cannot subscribe: MQTT client is not connected.");
            return false;
        }

        // Register an asynchronous event handler for received messages
        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            Debug.Log("Inside the callback - Message incoming");

            string receivedTopic = e.ApplicationMessage.Topic;
            byte[] payload = e.ApplicationMessage.PayloadSegment.ToArray(); ;
            string message = System.Text.Encoding.UTF8.GetString(payload);

            // Invoke the callback with the received topic and message
            onMessageReceived?.Invoke(receivedTopic, message);
            return;
        };

        // Build subscription options for the specified topic
        var mqttSubscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(builder =>
            {
                builder.WithTopic(topic);
            })
            .Build();

        // Attempt to subscribe to the topic
        await mqttClient.SubscribeAsync(mqttSubscribeOptions);
        return true;
    }

    #endregion

    #region Publish Methods
    /// <summary>Publishes a message to a specified topic.</summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="message">The message to send.</param>
    public async Task PublishMessage(string topic, string message)
    {
        // Check if the MQTT client is connected
        if (mqttClient == null || !mqttClient.IsConnected)
        {
            Debug.LogError("Cannot publish: MQTT client is not connected.");
            return;
        }

        // Build the MQTT message with the specified topic and payload
        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(message)
            .WithRetainFlag(false)
            .Build();

        // Publish the message asynchronously
        await mqttClient.PublishAsync(mqttMessage);
        Debug.Log($"Published message to topic {topic}: {message}");
    }

    #endregion

    #region Disconnect
    /// <summary>Disconnects the client from the MQTT broker.</summary>
    public async Task Disconnect()
    {
        // Check if the MQTT client is connected
        if (mqttClient == null || !mqttClient.IsConnected)
        {
            Debug.LogWarning("MQTT client is not connected. No action taken.");
            return;
        }

        // Attempt to disconnect the client
        try
        {
            await mqttClient.DisconnectAsync();
            Debug.Log("Successfully disconnected from MQTT broker.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during disconnect: {ex.Message}");
        }
    }

    #endregion

    #region Unsubscribe
    /// <summary>Unsubscribes from the specified MQTT topic.</summary>
    /// <param name="topics">A topic to unsubscribe from.</param>
    public async Task UnsubscribeFromTopics(string topic)
    {
        // Check if the MQTT client is connected
        if (mqttClient == null || !mqttClient.IsConnected)
        {
            Debug.LogError("Cannot unsubscribe: MQTT client is not connected.");
            return;
        }

        // Check if the topic name is valid
        if (topic == null || topic.Length == 0)
        {
            Debug.LogWarning("No topics provided for unsubscription.");
            return;
        }

        // Attempt to unsubscribe from topic
        try
        {
            await mqttClient.UnsubscribeAsync(topic);
            Debug.Log($"Successfully unsubscribed from topic: {topic}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during unsubscription: {ex.Message}");
        }
    }

    #endregion
}