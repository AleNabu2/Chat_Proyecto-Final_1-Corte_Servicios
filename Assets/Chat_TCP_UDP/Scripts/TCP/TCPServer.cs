using System; 
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TCPServer : MonoBehaviour, IServer
{
    private TcpListener tcpListener; // TCP server declaration
    private TcpClient connectedClient; // Connected client declaration
    private NetworkStream networkStream; // Network data stream

    const byte MESSAGE_TEXT = 0;
    const byte MESSAGE_IMAGE = 1;
    const byte MESSAGE_PDF = 2;
    public bool isServerRunning { get; private set; }

    public event Action<string> OnMessageReceived; //Send messages
    public event Action<Texture2D> OnImageReceived;
    public event Action<byte[], string> OnPdfReceived;
    public event Action OnConnected; //Connect into a chat
    public event Action OnDisconnected; //Disconnect into a chat

    public async Task StartServer(int port)
    {
        tcpListener = new TcpListener(IPAddress.Any, port); // Configures the TCP server to listen on any IP and the specified port
        tcpListener.Start(); // Starts the TCP server

        Debug.Log("[Server] Server of TCP started, waiting for connections..."); // Displays a message in the Unity console indicating that the server has started
        isServerRunning = true;

        connectedClient = await tcpListener.AcceptTcpClientAsync(); //The server start listens for incoming client connections asynchronously
        Debug.Log("[Server] Client connected: " + connectedClient.Client.RemoteEndPoint);
        OnConnected?.Invoke(); // Invokes the OnConnected event, notifying any subscribed listeners that a client has connected

        networkStream = connectedClient.GetStream();
        _ = ReceiveLoop();
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (connectedClient != null && connectedClient.Connected)
            {
                byte[] header = new byte[5];
                int headerRead = await ReadExact(networkStream, header, 5);

                if (headerRead == 0)
                {
                    Debug.Log("[Server] Client disconnected");
                    break;
                }

                byte messageType = header[0];
                int dataSize = BitConverter.ToInt32(header, 1);

                byte[] data = new byte[dataSize];
                int totalRead = 0;

                while (totalRead < dataSize)
                {
                    int bytesRead = await networkStream.ReadAsync(data, totalRead, dataSize - totalRead);

                    if (bytesRead == 0)
                        break;

                    totalRead += bytesRead;
                }

                if (totalRead != dataSize)
                {
                    Debug.LogError("Incomplete data received");
                    continue;
                }

                if (messageType == MESSAGE_TEXT)
                {
                    string message = Encoding.UTF8.GetString(data);
                    Debug.Log("[Server] Received: " + message);
                    OnMessageReceived?.Invoke("Client: " + message);
                }
                else if (messageType == MESSAGE_IMAGE)
                {
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(data);

                    Debug.Log("[Server] Image received");

                    OnImageReceived?.Invoke(texture);

                }
                else if (messageType == 2) 
                {
                    Debug.Log("[Server] PDF received");

                    OnPdfReceived?.Invoke(data, "received_file.pdf");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("ReceiveLoop Error: " + e);
        }
        finally
        {
            Disconnect();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        byte[] textBytes = Encoding.UTF8.GetBytes(message);

        byte[] packet = new byte[1 + 4 + textBytes.Length];

        packet[0] = MESSAGE_TEXT; 

        Array.Copy(BitConverter.GetBytes(textBytes.Length), 0, packet, 1, 4);
        Array.Copy(textBytes, 0, packet, 5, textBytes.Length);

        await networkStream.WriteAsync(packet, 0, packet.Length);
    }

    public async Task SendImage(Texture2D image)
    {
        byte[] imageBytes = image.EncodeToJPG(75);

        byte[] packet = new byte[1 + 4 + imageBytes.Length];

        packet[0] = MESSAGE_IMAGE; 

        Array.Copy(BitConverter.GetBytes(imageBytes.Length), 0, packet, 1, 4);
        Array.Copy(imageBytes, 0, packet, 5, imageBytes.Length);

        await networkStream.WriteAsync(packet, 0, packet.Length);
    }

    public async Task SendPDF(byte[] pdfBytes)
    {
        byte[] packet = new byte[1 + 4 + pdfBytes.Length];

        packet[0] = MESSAGE_PDF;

        Array.Copy(BitConverter.GetBytes(pdfBytes.Length), 0, packet, 1, 4);
        Array.Copy(pdfBytes, 0, packet, 5, pdfBytes.Length);

        await networkStream.WriteAsync(packet, 0, packet.Length);
    }

    private async Task<int> ReadExact(NetworkStream stream, byte[] buffer, int size)
    {
        int totalRead = 0;

        while (totalRead < size)
        {
            int bytesRead = await stream.ReadAsync(buffer, totalRead, size - totalRead);

            if (bytesRead == 0)
                return 0;

            totalRead += bytesRead;
        }

        return totalRead;
    }

    public void Disconnect() // Closes the connection to the client and cleans up resources
    {
        networkStream?.Close();
        connectedClient?.Close();

        networkStream = null;
        connectedClient = null;

        Debug.Log("[Server] Disconnected");
        OnDisconnected?.Invoke(); // Invokes the OnDisconnected event, notifying any subscribed listeners that the client has disconnected
    }

    private async void OnDestroy()
    {
        Disconnect();
        await Task.Delay(100);
    }
}

