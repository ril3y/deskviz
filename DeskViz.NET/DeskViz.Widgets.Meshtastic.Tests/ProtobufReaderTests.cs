using System;
using System.Text;
using Xunit;
using DeskViz.Widgets.Meshtastic.Protocol;

namespace DeskViz.Widgets.Meshtastic.Tests
{
    public class ProtobufReaderTests
    {
        [Fact]
        public void ReadVarint_SingleByte_ReturnsCorrectValue()
        {
            var data = new byte[] { 0x05 };
            var reader = new ProtobufReader(data);

            var result = reader.ReadVarint();

            Assert.Equal(5u, result);
        }

        [Fact]
        public void ReadVarint_MultiByte_ReturnsCorrectValue()
        {
            // 300 = 0xAC 0x02 in varint encoding
            var data = new byte[] { 0xAC, 0x02 };
            var reader = new ProtobufReader(data);

            var result = reader.ReadVarint();

            Assert.Equal(300u, result);
        }

        [Fact]
        public void ReadString_ValidString_ReturnsCorrectValue()
        {
            var testString = "Hello";
            var stringBytes = Encoding.UTF8.GetBytes(testString);
            var data = new byte[1 + stringBytes.Length];
            data[0] = (byte)stringBytes.Length;
            Array.Copy(stringBytes, 0, data, 1, stringBytes.Length);

            var reader = new ProtobufReader(data);
            var result = reader.ReadString();

            Assert.Equal(testString, result);
        }

        [Fact]
        public void ReadFixed32_ValidData_ReturnsCorrectValue()
        {
            var data = BitConverter.GetBytes(12345u);
            var reader = new ProtobufReader(data);

            var result = reader.ReadFixed32();

            Assert.Equal(12345u, result);
        }

        [Fact]
        public void ReadFloat_ValidData_ReturnsCorrectValue()
        {
            var data = BitConverter.GetBytes(3.14f);
            var reader = new ProtobufReader(data);

            var result = reader.ReadFloat();

            Assert.Equal(3.14f, result, 2);
        }

        [Fact]
        public void ReadTag_ValidTag_ReturnsFieldNumberAndWireType()
        {
            // Field 1, wire type 0 (varint) = 0x08
            var data = new byte[] { 0x08 };
            var reader = new ProtobufReader(data);

            var (fieldNumber, wireType) = reader.ReadTag();

            Assert.Equal(1, fieldNumber);
            Assert.Equal(0, wireType);
        }

        [Fact]
        public void ReadTag_Field2LengthDelimited_ReturnsCorrectValues()
        {
            // Field 2, wire type 2 (length-delimited) = 0x12
            var data = new byte[] { 0x12 };
            var reader = new ProtobufReader(data);

            var (fieldNumber, wireType) = reader.ReadTag();

            Assert.Equal(2, fieldNumber);
            Assert.Equal(2, wireType);
        }

        [Fact]
        public void HasMore_EmptyData_ReturnsFalse()
        {
            var reader = new ProtobufReader(Array.Empty<byte>());

            Assert.False(reader.HasMore);
        }

        [Fact]
        public void HasMore_WithData_ReturnsTrue()
        {
            var reader = new ProtobufReader(new byte[] { 0x01 });

            Assert.True(reader.HasMore);
        }
    }
}
