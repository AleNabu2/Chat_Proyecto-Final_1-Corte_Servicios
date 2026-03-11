using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UDPClient : MonoBehaviour, IClient
{
    private UdpClient udpClient; // UDP client to handle network communication
    private IPEndPoint remoteEndPoint; // Endpoint to identify the remote server
    public bool isServerConnected = false; // Flag to check if the client is connected to the server

    public event Action<string> OnMessageReceived;
    public event Action<Texture2D> OnImageReceived;
    public event Action<byte[], string> OnPdfReceived;

    Dictionary<string, byte[][]> imageChunks = new Dictionary<string, byte[][]>();
    Dictionary<string, int> imageChunkCounter = new Dictionary<string, int>();
    Dictionary<string, byte[][]> pdfChunks = new Dictionary<string, byte[][]>();
    Dictionary<string, int> pdfChunkCounter = new Dictionary<string, int>();
    Dictionary<string, string> pdfNames = new Dictionary<string, string>();

    public event Action OnConnected;
    public event Action OnDisconnected;

    public bool isConnected { get; private set; }

    public async Task ConnectToServer(string ipAddress, int port)
    {
        udpClient = new UdpClient(); // Creates a new instance of the UdpClient class
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);//The remote endpoint is the server's IP address and port number that the client will connect to

        isConnected = true;
        _ = ReceiveLoop(); // Starts the receive loop in a separate task to continuously listen for incoming messages from the server without blocking the main thread

        await SendMessageAsync("CONNECT"); // Sends an initial message to the server to confirm the handshake
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (isConnected)
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();// Waits for incoming messages from the server asynchronously
                string message = Encoding.UTF8.GetString(result.Buffer);

                if (message == "CONNECTED")
                {
                    Debug.Log("[Client] Server Answered");
                    OnConnected?.Invoke(); // Invokes the OnConnected event, notifying any subscribed listeners that a client has connected
                    continue; // Skip the rest of the loop and wait for the next message
                }
                if (message.StartsWith("IMG|"))
                {
                    string[] parts = message.Split('|');

                    string fileId = parts[1];
                    int index = int.Parse(parts[2]);
                    int total = int.Parse(parts[3]);
                    byte[] chunk = Convert.FromBase64String(parts[4]);

                    Debug.Log($"[CLIENT] Chunk recibido {index + 1}/{total} para imagen {fileId}");

                    if (!imageChunks.ContainsKey(fileId))
                    {
                        imageChunks[fileId] = new byte[total][];
                        imageChunkCounter[fileId] = 0;
                    }

                    imageChunks[fileId][index] = chunk;
                    imageChunkCounter[fileId]++;

                    if (imageChunkCounter[fileId] == total)
                    {
                        Debug.Log("[CLIENT] Todos los chunks recibidos. Reconstruyendo imagen...");
                        List<byte> data = new List<byte>();

                        foreach (var c in imageChunks[fileId])
                            data.AddRange(c);

                        Texture2D tex = new Texture2D(2, 2);
                        tex.LoadImage(data.ToArray());

                        Debug.Log("[CLIENT] Enviando imagen a UI");

                        Debug.Log("Image rebuilt successfully");
                        Debug.Log("IMAGE COMPLETED");

                        OnImageReceived?.Invoke(tex);

                        imageChunks.Remove(fileId);
                        imageChunkCounter.Remove(fileId);
                    }

                    continue;
                }

                if (message.StartsWith("PDF|"))
                {
                    string[] parts = message.Split('|');

                    string fileId = parts[1];
                    string fileName = parts[2];
                    int index = int.Parse(parts[3]);
                    int total = int.Parse(parts[4]);
                    byte[] chunk = Convert.FromBase64String(parts[5]);

                    if (!pdfChunks.ContainsKey(fileId))
                    {
                        pdfChunks[fileId] = new byte[total][];
                        pdfChunkCounter[fileId] = 0;
                        pdfNames[fileId] = fileName;
                    }

                    pdfChunks[fileId][index] = chunk;
                    pdfChunkCounter[fileId]++;

                    if (pdfChunkCounter[fileId] == total)
                    {
                        List<byte> data = new List<byte>();

                        foreach (var c in pdfChunks[fileId])
                            data.AddRange(c);

                        OnPdfReceived?.Invoke(data.ToArray(), pdfNames[fileId]);

                        pdfChunks.Remove(fileId);
                        pdfChunkCounter.Remove(fileId);
                        pdfNames.Remove(fileId);
                    }

                    continue;
                }

                Debug.Log("[Client] Received: " + message);
                OnMessageReceived?.Invoke(message);//Invokes the OnMessageReceived event, passing the received message to any subscribed listeners
            }
        }
        finally
        {
            Disconnect();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (!isConnected) // Checks if there is an active connection to the server
        {
            Debug.Log("[Client] Not connected to server."); 
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(message);// Converts the message string into a byte array
        await udpClient.SendAsync(data, data.Length, remoteEndPoint); // Sends the byte array to the server using UDP asynchronously

        Debug.Log("[Client] Sent: " + message);

    }

    public async Task SendImage(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToPNG();

        int chunkSize = 512;
        int totalChunks = Mathf.CeilToInt((float)imageBytes.Length / chunkSize);

        string fileId = Guid.NewGuid().ToString();

        for (int i = 0; i < totalChunks; i++)
        {
            int start = i * chunkSize;
            int length = Mathf.Min(chunkSize, imageBytes.Length - start);

            byte[] chunk = new byte[length];
            Array.Copy(imageBytes, start, chunk, 0, length);

            string base64 = Convert.ToBase64String(chunk);

            string message = $"IMG|{fileId}|{i}|{totalChunks}|{base64}";

            await SendMessageAsync(message);
            await Task.Delay(5);
        }
    }

    public async Task SendPDF(byte[] pdfBytes, string fileName)
    {
        int chunkSize = 512;
        int totalChunks = Mathf.CeilToInt((float)pdfBytes.Length / chunkSize);

        string fileId = Guid.NewGuid().ToString();

        for (int i = 0; i < totalChunks; i++)
        {
            int start = i * chunkSize;
            int length = Mathf.Min(chunkSize, pdfBytes.Length - start);

            byte[] chunk = new byte[length];
            Array.Copy(pdfBytes, start, chunk, 0, length);

            string base64 = Convert.ToBase64String(chunk);

            string message = $"PDF|{fileId}|{fileName}|{i}|{totalChunks}|{base64}";

            await SendMessageAsync(message);
            await Task.Delay(5);
        }
    }

    public void Disconnect()
    {
        if (!isConnected)
        {
            Debug.Log("[Client] The client is not connected");
            return;
        }
            
        isConnected = false;

        udpClient?.Close();
        udpClient?.Dispose();// Closes the UDP client and releases any resources associated with it
        udpClient = null;

        Debug.Log("[Client] Disconnected");
        OnDisconnected?.Invoke();// Invokes the OnDisconnected event, notifying any subscribed listeners that the client has disconnected from the server
    }

    private async void OnDestroy()
    {
        Disconnect();
        await Task.Delay(100);
    }
}
