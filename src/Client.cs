using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SCENetCore;

namespace SCENetGame;

internal static class Client
{
    private static Socket _socket;

    private static readonly Thread ReceiveThread = new(Receive);

    private static readonly ManualResetEvent ReceiveDone = new(false);

    public static event Action<string> ReceiveChat;
    public static event Action OnDisconnect;

    public static TextWriter ErrorOut { get; set; } = Console.Out;
    public static string Username { get; set; }

    public static bool TryConnect(string hostName, int port)
    {
        try
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);

            IPAddress ip = hostEntry.AddressList[0];

            var endPoint = new IPEndPoint(ip, port);

            _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _socket.Connect(endPoint);

            return true;
        }
        catch (Exception ex)
        {
            PrintError($"Failed to connect to {hostName}", ex);
            return false;
        }
    }

    public static void Send(byte[] buffer)
    {
        try
        {
            if (buffer.Length > Constants.BufferSize)
            {
                PrintError("Message is too long.");
                return;
            }

            _socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
        }
        catch (Exception ex)
        {
            PrintError("Error starting send", ex);
        }
    }

    public static void SendChat(string text)
    {
        Send(Translation.ToBytes(MessageType.Chat, text));
    }

    public static void StartReceiveThread()
    {
        ReceiveThread.Start();
    }

    private static void SendCallback(IAsyncResult result)
    {
        try
        {
            _socket.EndSend(result);
        }
        catch (Exception ex)
        {
            PrintError("Failed to send", ex);
        }
    }

    private static void Receive()
    {
        byte[] buffer = new byte[Constants.BufferSize];
        
        while (_socket.Connected)
        {
            try
            {
                ReceiveDone.Reset();

                _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, buffer);

                ReceiveDone.WaitOne();
            }
            catch (Exception ex)
            {
                PrintError("Failed to start receive", ex);
                return;
            }
        }
    }

    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            int receivedBytes = _socket.EndReceive(ar);

            ReceiveDone.Set();

            if (ar.AsyncState == null)
            {
                PrintError("State was null.");
                return;
            }

            byte[] buffer = (byte[])ar.AsyncState;

            var type = (MessageType)buffer[0];

            switch (type)
            {
            case MessageType.Chat:
                ReceiveChat?.Invoke(Translation.ToString(buffer, receivedBytes));
                break;
            default:
                PrintError($"Unknown message type: {type}.");
                return;
            }
        }
        catch (Exception ex)
        {
            ReceiveDone.Set();

            if (!_socket.Connected)
            {
                HandleDisconnected();
                return;
            }
            
            PrintError("Failed to receive data", ex);
        }
    }

    private static void HandleDisconnected()
    {
        ErrorOut.WriteLine("Disconnected from server.");
        
        OnDisconnect?.Invoke();
    }

    private static void PrintError(string message)
    {
        ErrorOut.WriteLine($"[ERROR] {message}");
    }

    private static void PrintError(string message, Exception ex)
    {
        PrintError($"{message}: {ex.Message}");
    }
}