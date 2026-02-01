using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SCENetGame;

internal static class Client
{
    private const int BufferSize = 1024;

    private static Socket _socket;

    private static readonly Thread _receiveThread = new(ReceiveThread);

    private static readonly ManualResetEvent _receiveDone = new(false);

    public static event Action<string> Receive;

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

    public static void Send(string message)
    {
        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            if (buffer.Length > BufferSize)
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

                byte[] buffer = new byte[BufferSize];

                _socket.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, buffer);

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

            var buffer = (byte[])ar.AsyncState;

            string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);

            Receive?.Invoke(message);
        }
        catch (Exception ex)
        {
            PrintError("Failed to receive data", ex);
        }
    }

    private static void PrintError(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }

    private static void PrintError(string message, Exception ex)
    {
        PrintError($"{message}: {ex.Message}");
    }
}