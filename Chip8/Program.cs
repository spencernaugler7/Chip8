
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

using System.Buffers.Binary;
using System.Drawing;
using System.Runtime.InteropServices.JavaScript;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using Window = Silk.NET.Windowing.Window;

namespace Chip8;

public class Chip8
{
    private static GL _gl;
    private const int ProgramLoadOffset = 0x200; // start of user data everything before this address is font data
    private const double Chip8InstructionFrequency = 1 / 700D;
    private double _emuTimingCounter = 0; // used to match the run cycle of every time this hits 14 ms parse a new instruction
    
    private static readonly byte[,] Fonts = new byte[16,5]
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
    
    private readonly IWindow window;
    private readonly byte[] memory = new byte[4096];
    private int pc = ProgramLoadOffset;
    private ushort iRegister = 0;
    private Stack<short> addressStack = new();
    private byte delayTimer = Byte.MinValue;
    private byte soundTimer = Byte.MinValue;
    // how the fuck are the timers represented
    
    // chip 8 variable registers
    private Dictionary<ushort, byte> _chip8RegisterDict = new()
    {
        {0, byte.MinValue},
        {1, byte.MinValue},
        {2, byte.MinValue},
        {3, byte.MinValue},
        {4, byte.MinValue},
        {5, byte.MinValue},
        {6, byte.MinValue},
        {7, byte.MinValue},
        {8, byte.MinValue},
        {9, byte.MinValue},
        {10, byte.MinValue},
        {0xA, byte.MinValue},
        {0xB, byte.MinValue},
        {0xC, byte.MinValue},
        {0xD, byte.MinValue},
        {0xE, byte.MinValue},
        {0xF, byte.MinValue},
    };

    private void OnRender(double deltaTime)
    {
        _emuTimingCounter += deltaTime;
        if (_emuTimingCounter >= Chip8InstructionFrequency)
        {
            Chip8MainLoop();
            _emuTimingCounter = 0;
        }
    }

    /// <summary>
    /// Main emulation loop for chip 8
    /// steps: Fetch, decode, execute
    /// </summary>
    private void Chip8MainLoop()
    {
        ushort currentChip8Instruction = BinaryPrimitives.ReadUInt16BigEndian(new[]{memory[pc] , memory[pc + 1]});
        int opcode = ExtractFromShort(currentChip8Instruction, 0, 4); // opcode is in the first nibble (half-byte)
        switch (opcode)
        {
            case 0x0: // chip 8 clear instruction
                _gl.ClearColor(Color.Black);
                break;
            case 0x1: // chip 8 jump instruction, jump destination is all the data after the opcode (12 bits long)
                int dest = ExtractFromShort(currentChip8Instruction, 4, 12); //  
                pc = dest;
                break;
            case 0x6: // set one of chip8's 15 built-in registers to 'newValue' 
                ushort destinationRegister = ExtractFromShort(currentChip8Instruction, 4, 4);
                ushort newValue = ExtractFromShort(currentChip8Instruction, 8, 8);
                _chip8RegisterDict[destinationRegister] = (byte)newValue;
                break;
            case 0x7: // add 'newValue' to one of chip8's registers
                ushort registerToAddTo = ExtractFromShort(currentChip8Instruction, 4, 4);
                ushort valueToAdd = ExtractFromShort(currentChip8Instruction, 8, 8);
                _chip8RegisterDict[registerToAddTo] += BitConverter.GetBytes(valueToAdd).First();
                break;
            case 0xD: // display instruction
                int xRegister = ExtractFromShort(currentChip8Instruction, 4, 4);
                int yRegister = ExtractFromShort(currentChip8Instruction, 8, 4);
                int height = ExtractFromShort(currentChip8Instruction, 12, 4);
                DisplayScreen(xRegister, yRegister, height);
                break;
        }

        pc += 2; // every chip 8 instruction is two bytes long, so we need to increment by two bytes
    }

    private void DisplayScreen(int xRegister, int yRegister, int height)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Helper function to get a bit array value from a ushort
    /// </summary>
    /// <param name="source">source short to extract a value from</param>
    /// <param name="start">start location of the bitvector to extract</param>
    /// <param name="length">length of the bitvector to extract</param>
    /// <returns>ushort representation of the extracted data</returns>
    private ushort ExtractFromShort(ushort source, int start, int length)
    {
        ushort clone = source;
        clone <<= start - 1;
        clone >>= 16 - length;
        return clone;
    }

    private void Onload()
    {
        _gl = window.CreateOpenGL();
        _gl.ClearColor(Color.Black);
    }
    
    private Chip8(IWindow window, byte[] instructions)
    {
        this.window = window;
        this.window.Render += OnRender;
        this.window.Load += Onload;

        //load up memory
        foreach(byte fontByte in Fonts)
        {
            memory.Append(fontByte);
        }
        foreach(byte instruction in instructions)
        {
            memory.Append(instruction);
        }
    }
    
    public void Run()
    {
        window.Run();
    }
    
    public class Builder
    {
        private const int Width = 64;
        private const int Height = 32;
        private int _scale = 8;
        private string _title = "Chip8 Emulator";
        private IWindow _window;
        private byte[] _instructions;

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
        
        public Builder WithInstructions(byte[] instructions)
        {
            _instructions = instructions;
            return this;
        }

        public Chip8 Build()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(Width, Height) * _scale;
            options.FramesPerSecond = 60;
            options.Title = _title;
        
            _window = Window.Create(options);

            return new Chip8(_window, _instructions);
        }

    }
}

public static class Program
{
    private static void Main(string[] args)
    {
        var chip8FilePath = Path.Join(Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName,"ibm_logo.ch8");
        byte[] data = File.ReadAllBytes(chip8FilePath);

        Chip8 chip8 = new Chip8.Builder()
            .WithScale(8)
            .WithTitle("Hello World!")
            .WithInstructions(data)
            .Build();

        chip8.Run();
    }
}
