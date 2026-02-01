using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SCENetCore;

namespace SCENetGame;

internal static class Client
{
    private static Socket _socket;

    private static readonly Thread _receiveThread = new(ReceiveThread);

    private static readonly ManualResetEvent _receiveDone = new(false);

    public static event Action<string> ReceiveChat;

    public static TextWriter ErrorOut { get; set; } = Console.Out;
    public static string Username { get; set; }

    public static bool TryConnect(string hostName, int port)
    {
        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            var endPoint = new IPEndPoint(addresses[0], port);

            _socket = new Socket(addresses[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);

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

    public static void StartReceive()
    {
        _receiveThread.Start();
    }

    private static void SendCallback(IAsyncResult result)
    {
        try
        {
            int sentBytes = _socket.EndSend(result);
        }
        catch (Exception ex)
        {
            PrintError("Faileed to send", ex);
        }
    }

    private static void ReceiveThread()
    {
        while (true)
        {
            try
            {
                _receiveDone.Reset();

                byte[] buffer = new byte[Constants.BufferSize];

                _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, buffer);

                _receiveDone.WaitOne();
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

            _receiveDone.Set();

            if (receivedBytes == 0)
            {
                return;
            }

            var buffer = (byte[])ar.AsyncState;

            var type = (MessageType)buffer[0];

            switch (type)
            {
            case MessageType.Chat:
                ReceiveChat?.Invoke(Translation.ToString(buffer, receivedBytes));
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(MessageType));
            }
        }
        catch (Exception ex)
        {
            PrintError("Failed to receive data", ex);
        }
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