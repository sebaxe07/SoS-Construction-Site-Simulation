using System;
using System.Collections; // Per IEnumerator
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class MQTTMessage
{
    // Variabili private
    private string machineId;
    private string task;
    private float[] coord;
    private string _msgid;

    public bool IsMessageInQueue(Queue<MQTTMessage> messageQueue)
    {
        foreach (MQTTMessage message in messageQueue)
        {
            if (message.MsgId == this.MsgId)
            {
                return true;
            }
        }
        return false;
    }

    // Getter e setter pubblici
    public string MachineId
    {
        get => machineId;
        set => machineId = value;
    }

    public string Task
    {
        get => task;
        set => task = value;
    }

    public float[] Coord
    {
        get => coord;
        set => coord = value;
    }

    public string MsgId
    {
        get => _msgid;
        set => _msgid = value;
    }
}
