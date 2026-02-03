using SCENeo.Node.Collision;
using SCENetCore;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace SCENetGame;

internal static class Client
{
    //private const int MaxArraysPerBucket = 8;

    private static Socket _socket;

    private static readonly Thread _receiveThreaed = new(Receive);

    private static readonly ManualResetEvent _receiveDone = new(false);

    //private static readonly ArrayPool<byte> _buffers = ArrayPool<byte>.Create(Constants.BufferSize, MaxArraysPerBucket);

    public static event Action<string> ReceiveChat;
    public static event Action OnDisconnect;

    public static TextWriter ErrorOut { get; set; } = Console.Out;
    public static string Username { get; set; }

    public static bool TryConnect(string hostName, int port)
    {
        try
        {
            IPAddress ip = Dns.GetHostAddresses(hostName)[0];

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
        _receiveThreaed.Start();
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
                _receiveDone.Reset();

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
        byte[] buffer = (byte[])ar.AsyncState;

        try
        {
            int received = _socket.EndReceive(ar);

            _receiveDone.Set();

            HandleReceive(buffer, received);
        }
        catch (Exception ex)
        {
            _receiveDone.Set();

            if (!_socket.Connected)
            {
                HandleDisconnected();
                return;
            }
            
            PrintError("Failed to receive data", ex);
        }
    }

    private static void HandleReceive(byte[] buffer, int received)
    {
        var type = (MessageType)buffer[0];

        switch (type)
        {
        case MessageType.Accept:
            OnAccept(Translation.ToString(buffer, received));
            break;
        case MessageType.Chat:
            ReceiveChat?.Invoke(Translation.ToString(buffer, received));
            break;
        default:
            PrintError($"Unknown message type: {type}.");
            break;
        }
    }

    private static void OnAccept(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            PrintError("Accept username was blank.");
            return;
        }

        Username = message;
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