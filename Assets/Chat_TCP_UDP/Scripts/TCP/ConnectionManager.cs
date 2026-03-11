using System;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public TCPServer tcpServer;
    public TCPClient tcpClient;

    public int port = 5555;
    public string serverIP = "127.0.0.1";

    async void Start()
    {
        try
        {
            await tcpServer.StartServer(port);
            Debug.Log("I am the SERVER");
        }
        catch (Exception)
        {
            Debug.Log("Server already exists, connecting as CLIENT");

            await tcpClient.ConnectToServer(serverIP, port);
            Debug.Log("I am the CLIENT");
        }
    }
}