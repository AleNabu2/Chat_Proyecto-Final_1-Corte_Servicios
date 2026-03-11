using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TCPClient : MonoBehaviour, IClient
{
    private TcpClient tcpClient;
    private NetworkStream networkStream;

    const byte MESSAGE_TEXT = 0;
    const byte MESSAGE_IMAGE = 1;
    const byte MESSAGE_PDF = 2;
    public bool isConnected { get; private set; }

    public event Action<string> OnMessageReceived;
    public event Action<Texture2D> OnImageReceived;
    public event Action<byte[], string> OnPdfReceived;

    public event Action OnConnected;
    public event Action OnDisconnected;

    public async Task ConnectToServer(string ip, int port)
    {
        tcpClient = new TcpClient(); //Creates a new instance of the TcpClient class

        await tcpClient.ConnectAsync(ip, port); //Asynchronously connects to the server at the specified IP address and port number
        networkStream = tcpClient.GetStream();// Retrieves the network stream associated with the connected TCP client

        isConnected = true;
        Debug.Log("[Client] Connected to server");
        OnConnected?.Invoke(); // Invokes the OnConnected event, notifying any subscribed listeners that the client has successfully connected to the server

        _ = ReceiveLoop(); //Starts the receive loop in a separate task to continuously listen for incoming messages from the server without blocking the main thread
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (isConnected)
            {
                byte[] header = new byte[5];
                int headerRead = await ReadExact(networkStream, header, 5);

                if (headerRead == 0)
                {
                    Debug.Log("[Client] Disconnected from server");
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
                    {
                        Debug.Log("[Client] Disconnected from server");
                        break;
                    }

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

                    Debug.Log("[Client] Received: " + message);

                    OnMessageReceived?.Invoke(message);
                }
                else if (messageType == MESSAGE_IMAGE)
                {
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(data);

                    Debug.Log("[Client] Image received");

                    OnImageReceived?.Invoke(texture);
                }
                else if (messageType == MESSAGE_PDF)
                {
                    Debug.Log("[Client] PDF received");

                    OnPdfReceived?.Invoke(data, "received_file.pdf");
                }
            }
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

        packet[0] = 0;

        Array.Copy(BitConverter.GetBytes(textBytes.Length), 0, packet, 1, 4);
        Array.Copy(textBytes, 0, packet, 5, textBytes.Length);

        await networkStream.WriteAsync(packet, 0, packet.Length);
    }

    public async Task SendImage(Texture2D image)
    {
        byte[] imageBytes = image.EncodeToPNG();

        byte[] packet = new byte[1 + 4 + imageBytes.Length];

        packet[0] = 1; 

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

    public void Disconnect()// Closes the connection to the server and cleans
    {
        isConnected = false;

        networkStream?.Close();
        tcpClient?.Close();

        networkStream = null;
        tcpClient = null;

        OnDisconnected?.Invoke(); // Invokes the OnDisconnected event, notifying any subscribed listeners that the client has disconnected from the server
        Debug.Log("[Client] Disconnected");
    }

    private async void OnDestroy()
    {
        Disconnect();
        await Task.Delay(100);
    }
}

