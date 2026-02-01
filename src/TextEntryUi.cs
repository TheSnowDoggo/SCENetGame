using SCENeo;
using SCENeo.Ui;
using System.Diagnostics;

namespace SCENetGame;

internal sealed class TextEntryUi : IRenderSource
{
    private const int MaxCharacters = 60;

    private const double BlinkDelay = 0.5;

    private const string PreText = "Enter chat: ";

    private readonly TextEntry _entry = new()
    {
        MaxCharacters = MaxCharacters,
    };

    private readonly TextLabel _label = new()
    {
        Width = MaxCharacters,
        Height = 1,
        Anchor = Anchor.Bottom,
    };

    private readonly DisplayMap _cursor = new(1, 1)
    {
        [0, 0] = new Pixel('\0', SCEColor.Black, SCEColor.White),
    };

    private readonly VirtualOverlay _overlay;

    private double _blinkTimer;

    public TextEntryUi()
    {
        _overlay = new VirtualOverlay()
        {
            Source  = _label,
            Overlay = _cursor,
        };

        _entry.Enter += Entry_OnEnter;

        _entry.TextChanged += Entry_OnTextChanged;
        _entry.IndexChanged += Entry_OnIndexChanged;

        SetPreText();
    }

    public void Update(double delta)
    {
        while (Console.KeyAvailable)
        {
            _entry.Next(Console.ReadKey(true));
        }

        if (_blinkTimer > 0)
        {
            _blinkTimer -= delta;
            return;
        }

        _blinkTimer = BlinkDelay;
        _cursor.Visible = !_cursor.Visible;
    }

    private void SetPreText()
    {
        _label.TextFgColor = SCEColor.DarkGray;
        _label.Text = PreText;
    }

    private void Entry_OnEnter()
    {
        string text = _entry.Text;

        Client.Send(text);

        Console.WriteLine($"<you> {text}");

        _entry.Clear();
    }

    private void Entry_OnTextChanged()
    {
        if (_entry.Length == 0)
        {
            SetPreText();
            return;
        }

        _label.Text = _entry.Text;
        _label.TextFgColor = SCEColor.Gray;
    }

    private void Entry_OnIndexChanged()
    {
        _cursor.Offset = new Vec2I(_entry.Index, 0);

        _blinkTimer = BlinkDelay;
        _cursor.Visible = true;
    }

    public IEnumerable<IRenderable> Render()
    {
        return [_overlay];
    }
}
