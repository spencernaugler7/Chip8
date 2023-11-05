using System.Buffers.Binary;
using System.Drawing;
using DotNext.Collections.Generic;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using Window = Silk.NET.Windowing.Window;

namespace Chip8;
// section chip8
public class Chip8
{
    // gl variables
    // Todo move gl concerns into custom window service
    private static GL gl;
    private uint vao; // vertex array object for gl
    private uint vbo; // vertex buffer object for gl
    private uint ebo; // element buffer object for gl
    private static uint glProgram;
    
    private const int FontOffset = 0x200; // start of user data everything before this address is font data
    private const double Chip8InstructionFrequency = 1 / 700D;
    private double emuTimingCounter = 0; // used to match the run cycle of every time this hits 14 ms parse a new instruction
    private readonly IWindow window;
    private byte[] memory = new byte[4096];
    private int pc = FontOffset;
    private ushort iRegister = 0;
    private Stack<short> addressStack = new();
    private byte delayTimer = Byte.MinValue;
    private byte soundTimer = Byte.MinValue;
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
        {0xA, byte.MinValue},
        {0xB, byte.MinValue},
        {0xC, byte.MinValue},
        {0xD, byte.MinValue},
        {0xE, byte.MinValue},
        {0xF, byte.MinValue},
    };

    private unsafe void OnRender(double deltaTime)
    {
        gl.BindVertexArray(vao);
        gl.UseProgram(glProgram);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*) 0);
        // emuTimingCounter += deltaTime;
        // if (emuTimingCounter >= Chip8InstructionFrequency)
        // {
        //     Chip8MainLoop();
        //     emuTimingCounter = 0;
        // }
    }

    /// <summary>
    /// Main emulation loop for chip 8
    /// steps: Fetch, decode, execute
    /// </summary>
    private void Chip8MainLoop()
    {
        ushort currentChip8Instruction = BinaryPrimitives.ReadUInt16BigEndian(new[]{memory[FontOffset + pc] , memory[FontOffset + pc + 1]});
        int opcode = ExtractFromShort(currentChip8Instruction, 0, 4); // opcode is in the first nibble (half-byte)
        switch (opcode)
        {
            case 0x0: // chip 8 clear instruction
                gl.ClearColor(Color.Black);
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

    // section Onload
    private unsafe void Onload()
    {
        float[] vertices =
        {
            0.5f, 0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f, 0.5f, 0.0f
        }; // draws a basic quad to the screen
        
        uint[] indices =
        {
            0u, 1u, 3u,
            1u, 2u, 3u
        };
        
        gl = window.CreateOpenGL();
        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);
        
        vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (float* buff = vertices)
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (vertices.Length * sizeof(float)), buff, BufferUsageARB.StaticDraw);

        ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (uint* buff = indices)
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (indices.Length * sizeof(uint)), buff, BufferUsageARB.StaticDraw);

        const string vertexShaderCode =
            """
            # version 330 core
            layout (location = 0) in vec3 aPosition;
            void main()
            {
                gl_Position = vec4(aPosition, 1.0);
            }
            """;
        const string fragmentShaderCode =
            """
            # version 330 core
            out vec4 out_color;
            void main()
            {
                out_color = vec4(1.0, 0.5, 0.2, 1.0);
            }
            """;
        uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, vertexShaderCode);

        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fragmentShaderCode);
        
        gl.CompileShader(vertexShader);
        gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
            throw new Exception("Vertex shader failed to compile" + gl.GetShaderInfoLog(vertexShader));

        gl.CompileShader(fragmentShader);
        gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
        if (fStatus != (int)GLEnum.True)
            throw new Exception("Fragment shader failed to compile" + gl.GetShaderInfoLog(fragmentShader));
        
        glProgram = gl.CreateProgram();
        
        gl.AttachShader(glProgram, vertexShader);
        gl.AttachShader(glProgram, fragmentShader);
        
        gl.LinkProgram(glProgram);
        gl.GetProgram(glProgram, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
            throw new Exception("Program failed to link: " + gl.GetProgramInfoLog(glProgram));
        
        gl.DetachShader(glProgram, vertexShader);
        gl.DetachShader(glProgram, fragmentShader);
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);

        const uint positionLoc = 0;
        gl.EnableVertexAttribArray(positionLoc);
        gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*) 0);
        
        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        
        //finally draw shit
        
    }
    
    private Chip8 (IWindow window, byte[] instructions)
    {
        byte[] fonts = {
             0xF0, 0x90, 0x90, 0x90, 0xF0 , // 0
             0x20, 0x60, 0x20, 0x20, 0x70 , // 1
             0xF0, 0x10, 0xF0, 0x80, 0xF0 , // 2
             0xF0, 0x10, 0xF0, 0x10, 0xF0 , // 3
             0x90, 0x90, 0xF0, 0x10, 0x10 , // 4
             0xF0, 0x80, 0xF0, 0x10, 0xF0 , // 5
             0xF0, 0x80, 0xF0, 0x90, 0xF0 , // 6
             0xF0, 0x10, 0x20, 0x40, 0x40 , // 7
             0xF0, 0x90, 0xF0, 0x90, 0xF0 , // 8
             0xF0, 0x90, 0xF0, 0x10, 0xF0 , // 9
             0xF0, 0x90, 0xF0, 0x90, 0x90 , // A
             0xE0, 0x90, 0xE0, 0x90, 0xE0 , // B
             0xF0, 0x80, 0x80, 0x80, 0xF0 , // C
             0xE0, 0x90, 0x90, 0x90, 0xE0 , // D
             0xF0, 0x80, 0xF0, 0x80, 0xF0 , // E
             0xF0, 0x80, 0xF0, 0x80, 0x80   // F
        };
        
        this.window = window;
        this.window.Render += OnRender;
        this.window.Load += Onload;
        memory.Append(fonts);
        memory.Append(instructions);
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
        var chip8FilePath = Path.Join(Environment.CurrentDirectory, "Roms", "ibm_logo.ch8");
        byte[] data = File.ReadAllBytes(chip8FilePath);

        Chip8 chip8 = new Chip8.Builder()
            .WithScale(8)
            .WithTitle("Hello World!")
            .WithInstructions(data)
            .Build();

        chip8.Run();
    }
}
