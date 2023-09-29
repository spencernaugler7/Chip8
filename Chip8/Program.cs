
// Memory: CHIP-8 has direct access to up to 4 kilobytes of RAM
// Display: 64 x 32 pixels (or 128 x 64 for SUPER-CHIP) monochrome, ie. black or white
// A program counter, often called just “PC”, which points at the current instruction in memory
// One 16-bit index register called “I” which is used to point at locations in memory
// A stack for 16-bit addresses, which is used to call subroutines/functions and return from them
// An 8-bit delay timer which is decremented at a rate of 60 Hz (60 times per second) until it reaches 0
// An 8-bit sound timer which functions like the delay timer, but which also gives off a beeping sound as long as it’s not 0
// 16 8-bit (one byte) general-purpose variable registers numbered 0 through F hexadecimal, ie. 0 through 15 in decimal, called V0 through VF
// VF is also used as a flag register; many instructions will set it to either 1 or 0 based on some rule, for example using it as a carry flag

// each instruction is two bytes
// chip 8 instruction layout (nibble = half a byte)
// 
// X: The second nibble. Used to look up one of the 16 registers (VX) from V0 through VF.
// Y: The third nibble. Also used to look up one of the 16 registers (VY) from V0 through VF.
// N: The fourth nibble. A 4-bit number.

using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Chip8;

public class Chip8
{
    private const int ProgramLoadOffset = 0x200; // this is the start of user data everything before this address is font data
    private static readonly int[,] Fonts = new int[16,5]
    {
        { 0xF0, 0x90, 0x90, 0x90, 0xF0 }, // 0
        { 0x20, 0x60, 0x20, 0x20, 0x70 }, // 1
        { 0xF0, 0x10, 0xF0, 0x80, 0xF0 }, // 2
        { 0xF0, 0x10, 0xF0, 0x10, 0xF0 }, // 3
        { 0x90, 0x90, 0xF0, 0x10, 0x10 }, // 4
        { 0xF0, 0x80, 0xF0, 0x10, 0xF0 }, // 5
        { 0xF0, 0x80, 0xF0, 0x90, 0xF0 }, // 6
        { 0xF0, 0x10, 0x20, 0x40, 0x40 }, // 7
        { 0xF0, 0x90, 0xF0, 0x90, 0xF0 }, // 8
        { 0xF0, 0x90, 0xF0, 0x10, 0xF0 }, // 9
        { 0xF0, 0x90, 0xF0, 0x90, 0x90 }, // A
        { 0xE0, 0x90, 0xE0, 0x90, 0xE0 }, // B
        { 0xF0, 0x80, 0x80, 0x80, 0xF0 }, // C
        { 0xE0, 0x90, 0x90, 0x90, 0xE0 }, // D
        { 0xF0, 0x80, 0xF0, 0x80, 0xF0 }, // E
        { 0xF0, 0x80, 0xF0, 0x80, 0x80 }  // F
    };
    
    private IWindow _window;
    private byte[] _memory = new byte[4096];
    private UInt16 PC = UInt16.MinValue;
    private UInt16 IRegister = UInt16.MinValue;
    private Stack<UInt16> _addressStack = new();
    private byte DelayTimer = Byte.MinValue;
    private byte SoundTimer = Byte.MinValue;
    // how the fuck are the timers represented
    
    // chip 8 variable registers
    private byte V0 = byte.MinValue;
    private byte V1 = byte.MinValue;
    private byte V2 = byte.MinValue;
    private byte V3 = byte.MinValue;
    private byte V4 = byte.MinValue;
    private byte V5 = byte.MinValue;
    private byte V7 = byte.MinValue;
    private byte V8 = byte.MinValue;
    private byte V9 = byte.MinValue;
    private byte VA = byte.MinValue;
    private byte VB = byte.MinValue;
    private byte VC = byte.MinValue;
    private byte VD = byte.MinValue;
    private byte VE = byte.MinValue;
    private byte VF = byte.MinValue;

    private void OnLoad()
    {
        
    }

    private void OnRender(double deltaTime)
    {
        
    }

    private void OnUpdate(double deltaTime)
    {
        
    }
    
    private Chip8(IWindow window)
    {
        _window = window;
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Update += OnUpdate;
    }
    
    public class Builder
    {
        private const int Width = 64;
        private const int Height = 32;
        private int _scale = 8;
        private string _title = "Chip8 Emulator";
        private IWindow _window;
        
        public Builder WithScale(int scale) 
        {
            _scale = scale;
            return this;
        }

        public Builder WithTitle(string title)
        {
            _title = title;
            return this;
        }

        public Chip8 Build()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(Width, Height) * _scale;
            options.FramesPerSecond = 60;
            options.Title = _title;
        
            _window = Window.Create(options);

            return new Chip8(_window);
        }
    }

    public void Run()
    {
        _window.Run();
    }
}

public static class Program
{
    private static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Title = "Chip 8 Net!";

        var chip8 = new Chip8.Builder()
            .WithScale(8)
            .WithTitle("Hello World!")
            .Build();

        chip8.Run();
    }
}
