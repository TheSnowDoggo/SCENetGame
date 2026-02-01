using SCENeo;
using SCENeo.Ui;
using SCEWin;

namespace SCENetGame;

internal sealed class Canvas
{
    private readonly Display _display;

    public Canvas()
    {
        _display = new Display()
        {
            Renderable = Viewport,
            Output = WinOutput.Instance,
        };

        _display.OnResize += Display_OnResize;
    }

    public Viewport Viewport { get; } = new() { BasePixel = Pixel.DarkGray };

    public void Update()
    {
        _display.Update();
    }

    private void Display_OnResize(int width, int height)
    {
        Viewport.Width = width;
        Viewport.Height = height;
    }
}