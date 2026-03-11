using TMPro;
using UnityEngine;

public enum ChatProtocol
{
    Select,
    TCP,
    UDP
}

public class ProtocolSelector : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public TMP_Text portText;
    public TMP_Text ipText;
    public string ip = "127.0.0.1";
    public int port = 5555;

    void Start()
    {
        dropdown.onValueChanged.AddListener(delegate {
            PrintProtocol();
        });
        portText.text = "Port: " + port;
        ipText.text = "IP: " + ip;
    }
    public ChatProtocol SelectedProtocol
    {
        get { return (ChatProtocol)dropdown.value; }
    }

    public void PrintProtocol()
    {
        Debug.Log("Selected Protocol: " + SelectedProtocol);
    }
}