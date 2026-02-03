using SCENeo;
using SCENeo.Node;
using SCENeo.Node.Render;
using SCENeo.Ui;
using SCENetCore;
using System.Net;

namespace SCENetGame;

internal sealed class Game : IRenderSource
{
    private readonly Updater _updater = new()
    {
        FrameCap = 60,
    };

    private readonly Canvas _canvas = new();

    private readonly TextLabel _fps = new()
    {
        Width = 20,
        Height = 1,
    };

    private readonly UiConsole _console = new()
    {
        Width = 60,
        Height = 10,
        BufferWidth = 60,
        BufferHeight = 2000,
        Anchor = Anchor.Right,
    };

    private readonly ChatBox _chatBox = new ChatBox();

    private readonly RenderChannel _rc = new()
    {
        BasePixel = Pixel.DarkCyan,
    };

    private readonly RenderEngine _renderEngine;

    private readonly NodeTree _tree;

    public Game()
    {
        _updater.OnUpdate += Update;

        _renderEngine = new RenderEngine()
        {
            Channels = new() { { 0, _rc } }, 
        };

        _tree = new NodeTree()
        {
            Engines = [_renderEngine],
        };

        _canvas.Viewport.Source = new RenderManager()
        {
            Sources = [this, _chatBox],
        };
    }

    public IEnumerable<IRenderable> Render()
    {
        return [_rc, _console, _fps];
    }

    public void Run()
    {
        Console.WriteLine($"Hint: {Dns.GetHostName()}");
        
        ConnectToServer();

        Console.WriteLine("Connected to server successful!");

        Client.ReceiveChat += Client_OnReceive;

        Client.StartReceiveThread();

        SetUsername();

        Client.ErrorOut = _console;
        _chatBox.Out = _console;

        Console.CursorVisible = false;

        _updater.Start();
    }

    private void Update(double delta)
    {
        _chatBox.Update(delta);

        _fps.Text = $"FPS: {_updater.FPS:0}";

        _tree.Update(delta);

        _canvas.Update();
    }

    private void Client_OnReceive(string message)
    {
        _console.WriteLine(message);
    }

    private static void ConnectToServer()
    {
        while (true)
        {
            Console.Write("Enter server address (<hostname:port>): ");

            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Input was empty.");
                continue;
            }

            int delimiter = input.IndexOf(':');

            if (delimiter == -1)
            {
                Console.WriteLine("Missing port delimiter \':\'.");
                continue;
            }

            if (!int.TryParse(input[(delimiter + 1)..], out int port))
            {
                Console.WriteLine("Port was not a valid integer.");
                continue;
            }

            if (port is < 0 or > 65535)
            {
                Console.WriteLine("Port must be between 0-65535.");
                continue;
            }

            string hostName = input[..delimiter];

            Console.WriteLine($"Host: {hostName} Port: {port}");

            if (!Client.TryConnect(hostName, port))
            {
                continue;
            }

            return;
        }
    }

    private static void SetUsername()
    {
        while (true)
        {
            Console.Write("Enter username: ");
            string username = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(username))
            {
                Console.WriteLine("Input was empty.");
                continue;
            }

            if (username.Length is < 3 or > 16)
            {
                Console.WriteLine("Username must be between 3-16 characters.");
                continue;
            }

            Client.Send(Translation.ToBytes(MessageType.SetUsername, username));

            Client.Username = username;

            return;
        }
    }
}