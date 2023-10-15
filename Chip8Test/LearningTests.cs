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