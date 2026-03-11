using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class UDPServer : MonoBehaviour, IServer
{
    private UdpClient udpServer; // UDP client to handle network communication
    private IPEndPoint remoteEndPoint; // Endpoint to identify the remote client

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

    public bool isServerRunning { get; private set; }

    public Task StartServer(int port)
    {
        udpServer = new UdpClient(port);
        Debug.Log("[Server] Server of UDP started. Waiting for messages...");
        isServerRunning = true;

        _ = ReceiveLoop();
        return Task.CompletedTask;
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (isServerRunning)
            {
                UdpReceiveResult result = await udpServer.ReceiveAsync();// Waits for incoming messages from the server asynchronously
                remoteEndPoint = result.RemoteEndPoint;
                string message = Encoding.UTF8.GetString(result.Buffer);

                /*This part jus funtion first time that I connected in the chat*/
                if (message == "CONNECT")
                {
                    Debug.Log("[Server] Client connected: " + result.RemoteEndPoint);
                    remoteEndPoint = result.RemoteEndPoint;
                    await SendMessageAsync("CONNECTED"); // Sends a welcome message back to the client to confirm the handshake
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

                Debug.Log("[Server] Received: " + message);
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
        if (!isServerRunning) // Checks if there is an active connection to the server
        {
            Debug.Log("[Server] The server isn´t running");
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(message);// Converts the message string into a byte array
        await udpServer.SendAsync(data, data.Length, remoteEndPoint); // Sends the byte array to the server using UDP asynchronously

        Debug.Log("[Server] Sent: " + message);
    }

    public async Task SendImage(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToPNG();

        int chunkSize = 6000;
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
        int chunkSize = 6000;
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
        if (!isServerRunning)
        {
            Debug.Log("[Server] The server is not running");
            return;
        }

        isServerRunning = false;

        udpServer?.Close();
        udpServer?.Dispose();// Closes the UDP client and releases any resources associated with it
        udpServer = null;

        Debug.Log("[Server] Disconnected");
        OnDisconnected?.Invoke();// Invokes the OnDisconnected event, notifying any subscribed listeners that the client has disconnected from the server

    }

    private async void OnDestroy()
    {
        Disconnect();
        await Task.Delay(100); 
    }
}
