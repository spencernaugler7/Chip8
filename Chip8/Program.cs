// See https://aka.ms/new-console-template for more information

using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Chip8;

public static class Program
{
    private const int Scale = 20;

    private static void OnLoad()
    {

    }

    private static void OnRender(double obj)
    {

    }

    private static void OnUpdate(double obj)
    {

    }

    private static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(64, 32) * Scale;
        options.Title = "Chip 8 Net!";

        IWindow window = Window.Create(options);
        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;

        window.Run();
        window.Dispose();
    }
}