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
using NUnit.Framework.Constraints;

namespace Chip8Test;

[TestFixture]
public class LearningTests
{
    
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestConvertUshort16ToByteCanAddtoByte() // so convert everything away from bytes before calculations
    {
        ushort a = BitConverter.GetBytes(0b0000000000000001).First(); // originally thought I needed to use .Last() due to how the literal looks
        ushort b = 0b00000001;
        int c = b + a; // can't do math on ushorts for some reason, have to convert to int
        Assert.That(c, Is.EqualTo(0b00000010));
    }


    // section 
    [TestCase("ibm_logo.ch8", 0, 1, 0x00E0)]
    [TestCase("ibm_logo.ch8", 2, 3, 0xA22A)]
    public void Chip8ByteToInstructionTest(string fileName, int startPosition, int endPosition, int expectedResult) // ensure I am reading data correctly
    {
        string chip8FilePath = Path.Join(Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName, fileName);
        byte[] chip8Data = File.ReadAllBytes(chip8FilePath);
        ushort instruction = BinaryPrimitives.ReadUInt16BigEndian(new[] { chip8Data[startPosition], chip8Data[endPosition] }); // each instruction is a ushort which is two bytes
        Assert.That(instruction, Is.EqualTo(expectedResult));
    }
}