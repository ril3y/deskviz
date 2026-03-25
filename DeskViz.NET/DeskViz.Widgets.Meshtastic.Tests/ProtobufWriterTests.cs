using System;
using Xunit;
using DeskViz.Widgets.Meshtastic.Protocol;

namespace DeskViz.Widgets.Meshtastic.Tests
{
    public class ProtobufWriterTests
    {
        [Fact]
        public void WriteVarint_SingleByte_WritesCorrectly()
        {
            var writer = new ProtobufWriter();

            writer.WriteVarint(5);
            var result = writer.ToArray();

            Assert.Single(result);
            Assert.Equal(0x05, result[0]);
        }

        [Fact]
        public void WriteVarint_MultiByte_WritesCorrectly()
        {
            var writer = new ProtobufWriter();

            writer.WriteVarint(300);
            var result = writer.ToArray();

            Assert.Equal(2, result.Length);
            Assert.Equal(0xAC, result[0]);
            Assert.Equal(0x02, result[1]);
        }

        [Fact]
        public void WriteTag_Field1Varint_WritesCorrectly()
        {
            var writer = new ProtobufWriter();

            writer.WriteTag(1, 0); // Field 1, wire type 0
            var result = writer.ToArray();

            Assert.Single(result);
            Assert.Equal(0x08, result[0]);
        }

        [Fact]
        public void WriteTag_Field2LengthDelimited_WritesCorrectly()
        {
            var writer = new ProtobufWriter();

            writer.WriteTag(2, 2); // Field 2, wire type 2
            var result = writer.ToArray();

            Assert.Single(result);
            Assert.Equal(0x12, result[0]);
        }

        [Fact]
        public void WriteBytes_EmptyArray_WritesLengthOnly()
        {
            var writer = new ProtobufWriter();

            writer.WriteBytes(Array.Empty<byte>());
            var result = writer.ToArray();

            Assert.Single(result);
            Assert.Equal(0x00, result[0]);
        }

        [Fact]
        public void WriteBytes_WithData_WritesLengthAndData()
        {
            var writer = new ProtobufWriter();
            var data = new byte[] { 0x01, 0x02, 0x03 };

            writer.WriteBytes(data);
            var result = writer.ToArray();

            Assert.Equal(4, result.Length);
            Assert.Equal(0x03, result[0]); // Length
            Assert.Equal(0x01, result[1]);
            Assert.Equal(0x02, result[2]);
            Assert.Equal(0x03, result[3]);
        }

        [Fact]
        public void WriteBool_True_WritesOne()
        {
            var writer = new ProtobufWriter();

            writer.WriteBool(true);
            var result = writer.ToArray();

            Assert.Single(result);
            Assert.Equal(0x01, result[0]);
        }

        [Fact]
        public void WriteBool_False_WritesZero()
        {
            var writer = new ProtobufWriter();

            writer.WriteBool(false);
            var result = writer.ToArray();

            Assert.Single(result);
            Assert.Equal(0x00, result[0]);
        }
    }
}
