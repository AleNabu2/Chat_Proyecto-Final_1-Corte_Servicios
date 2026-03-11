using UnityEngine;
using TMPro;
using System;
using SFB;
using System.IO; 

public class ChatManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text ipText;
    public UIManager uiManager;
    public ProtocolSelector protocolSelector;

    [Header("TCP")]
    public TCPServer tcpServer;
    public TCPClient tcpClient;

    [Header("UDP")]
    public UDPServer udpServer;
    public UDPClient udpClient;

    [Header("Connection Settings")]
    public int port = 5555;
    void Start()
    {
        tcpClient.OnMessageReceived += uiManager.AddMessage;
        tcpServer.OnMessageReceived += uiManager.AddMessage;

        udpClient.OnMessageReceived += uiManager.AddMessage;
        udpServer.OnMessageReceived += uiManager.AddMessage;

        tcpClient.OnImageReceived += uiManager.AddImage;
        tcpServer.OnImageReceived += uiManager.AddImage;

        udpClient.OnImageReceived += uiManager.AddImage;
        udpServer.OnImageReceived += uiManager.AddImage;

        tcpClient.OnPdfReceived += uiManager.AddPdf;
        tcpServer.OnPdfReceived += uiManager.AddPdf;

        udpClient.OnPdfReceived += uiManager.AddPdf;
        udpServer.OnPdfReceived += uiManager.AddPdf;
    }
    public async void Connect()
    {
        string ip = ipText.text.Replace("IP:", "").Trim();

        if (protocolSelector.SelectedProtocol == ChatProtocol.TCP)
        {
            try
            {
                Debug.Log("Trying to connect as TCP CLIENT");

                await tcpClient.ConnectToServer(ip, port);

                Debug.Log("Running as TCP CLIENT");
                uiManager.ShowChat("CLIENT", protocolSelector.SelectedProtocol);
            }
            catch (Exception)
            {
                Debug.Log("No server found. Starting TCP SERVER");

                _ = tcpServer.StartServer(port);

                uiManager.ShowChat("SERVER", protocolSelector.SelectedProtocol);
            }
        }
        else
        {
            try
            {
                Debug.Log("Starting UDP SERVER");

                await udpServer.StartServer(port);

                uiManager.ShowChat("SERVER", protocolSelector.SelectedProtocol);
            }
            catch (Exception)
            {
                Debug.Log("Server already running, connecting as UDP CLIENT");

                await udpClient.ConnectToServer(ip, port);

                uiManager.ShowChat("CLIENT", protocolSelector.SelectedProtocol);
            }
        }
    }

    public async void SendChatMessage(string message)
    {
        if (protocolSelector.SelectedProtocol == ChatProtocol.TCP)
        {
            if (tcpClient.isConnected)
            {
                await tcpClient.SendMessageAsync(message);
            }
            else
            {
                await tcpServer.SendMessageAsync(message);
            }
        }
        else
        {
            if (udpClient.isConnected)
            {
                await udpClient.SendMessageAsync(message);
            }
            else if (udpServer.isServerRunning)
            {
                await udpServer.SendMessageAsync(message);
            }
        }
    }

    public async void SendImageFromFile()
    {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
        };

        var paths = StandaloneFileBrowser.OpenFilePanel("Select Image", "", extensions, false);

        if (paths.Length == 0)
            return;

        byte[] imageBytes = File.ReadAllBytes(paths[0]);

        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);

        uiManager.AddImage(texture);

        if (protocolSelector.SelectedProtocol == ChatProtocol.TCP)
        {
            if (tcpClient.isConnected)
            {
                await tcpClient.SendImage(texture);
            }
            else
            {
                await tcpServer.SendImage(texture);
            }
        }
        else
        {
            if (udpClient.isConnected)
            {
                await udpClient.SendImage(texture);
            }
            else if (udpServer.isServerRunning)
            {
                await udpServer.SendImage(texture);
            }
        }
    }

    public async void SendPdfFromFile()
    {
        var extensions = new[] { new ExtensionFilter("PDF Files", "pdf") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select PDF", "", extensions, false);

        if (paths.Length == 0) return;

        byte[] pdfBytes = File.ReadAllBytes(paths[0]);
        string fileName = Path.GetFileName(paths[0]);

        uiManager.AddPdf(pdfBytes, fileName);

        if (protocolSelector.SelectedProtocol == ChatProtocol.TCP)
        {
            if (tcpClient != null && tcpClient.isConnected)
            {
                await tcpClient.SendPDF(pdfBytes);
            }
            else if (tcpServer != null && tcpServer.isServerRunning)
            {
                await tcpServer.SendPDF(pdfBytes);
            }
        }
        else 
        {
            if (udpClient != null && udpClient.isConnected)
            {
                await udpClient.SendPDF(pdfBytes, fileName);
            }
            else if (udpServer != null && udpServer.isServerRunning)
            {
                await udpServer.SendPDF(pdfBytes, fileName);
            }
        }
    }

    public void CloseConnection()
    {
        if (tcpClient != null && tcpClient.isConnected)
            tcpClient.Disconnect();

        if (tcpServer != null && tcpServer.isServerRunning)
            tcpServer.Disconnect();

        if (udpClient != null && udpClient.isConnected)
            udpClient.Disconnect();

        if (udpServer != null && udpServer.isServerRunning)
            udpServer.Disconnect();
    }
}