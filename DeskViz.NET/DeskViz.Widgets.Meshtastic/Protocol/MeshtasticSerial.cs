using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace DeskViz.Widgets.Meshtastic.Protocol
{
    /// <summary>
    /// Handles serial communication with a Meshtastic device using the streaming protocol.
    /// Protocol: 4-byte header (0x94 0xc3 MSB LSB) followed by protobuf payload.
    /// </summary>
    public class MeshtasticSerial : IDisposable
    {
        private const byte START1 = 0x94;
        private const byte START2 = 0xc3;
        private const int MAX_PACKET_SIZE = 512;
        private const int DEFAULT_BAUD_RATE = 115200;

        private SerialPort? _serialPort;
        private CancellationTokenSource? _readCts;
        private Task? _readTask;
        private readonly object _lock = new();

        public event EventHandler<FromRadioEventArgs>? PacketReceived;
        public event EventHandler<string>? DebugOutput;
        public event EventHandler<Exception>? ErrorOccurred;
        public event EventHandler<bool>? ConnectionStateChanged;

        public bool IsConnected => _serialPort?.IsOpen ?? false;
        public string? PortName => _serialPort?.PortName;

        /// <summary>
        /// Gets available serial ports on the system
        /// </summary>
        public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

        /// <summary>
        /// Connects to a Meshtastic device on the specified serial port.
        /// Note: DTR/RTS causes device to reboot. Wait ~2 seconds after connect before requesting config.
        /// </summary>
        public Task<bool> ConnectAsync(string portName, int baudRate = DEFAULT_BAUD_RATE)
        {
            try
            {
                Disconnect();

                _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    DtrEnable = true,
                    RtsEnable = true
                };

                _serialPort.Open();

                // Start reading task
                _readCts = new CancellationTokenSource();
                _readTask = Task.Run(() => ReadLoopAsync(_readCts.Token));

                // Note: DTR/RTS causes device to reboot - wait before requesting config
                // Caller should wait 2 seconds then call RequestConfigAsync()

                ConnectionStateChanged?.Invoke(this, true);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                Disconnect();
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Disconnects from the Meshtastic device
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _readCts?.Cancel();
                _readTask?.Wait(1000);
            }
            catch { }

            _readCts?.Dispose();
            _readCts = null;
            _readTask = null;

            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Dispose();
                _serialPort = null;
                ConnectionStateChanged?.Invoke(this, false);
            }
        }

        /// <summary>
        /// Sends a ToRadio packet to the device
        /// </summary>
        public async Task SendPacketAsync(byte[] protobufData)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                throw new InvalidOperationException("Not connected to a Meshtastic device");

            if (protobufData.Length > MAX_PACKET_SIZE)
                throw new ArgumentException($"Packet too large: {protobufData.Length} > {MAX_PACKET_SIZE}");

            var packet = new byte[4 + protobufData.Length];
            packet[0] = START1;
            packet[1] = START2;
            packet[2] = (byte)(protobufData.Length >> 8);  // MSB
            packet[3] = (byte)(protobufData.Length & 0xFF); // LSB
            Array.Copy(protobufData, 0, packet, 4, protobufData.Length);

            lock (_lock)
            {
                _serialPort.Write(packet, 0, packet.Length);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Request device configuration (used to verify connection and get node info)
        /// </summary>
        public async Task RequestConfigAsync()
        {
            // ToRadio with want_config_id = 1
            var toRadio = new ToRadio { WantConfigId = 1 };
            await SendPacketAsync(toRadio.ToBytes());
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            var buffer = new List<byte>();
            var state = ReadState.WaitingForStart1;
            int expectedLength = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_serialPort == null || !_serialPort.IsOpen)
                    {
                        await Task.Delay(100, ct);
                        continue;
                    }

                    if (_serialPort.BytesToRead == 0)
                    {
                        await Task.Delay(10, ct);
                        continue;
                    }

                    int b = _serialPort.ReadByte();
                    if (b == -1) continue;

                    switch (state)
                    {
                        case ReadState.WaitingForStart1:
                            if (b == START1)
                            {
                                state = ReadState.WaitingForStart2;
                            }
                            else
                            {
                                // Debug output from device
                                DebugOutput?.Invoke(this, ((char)b).ToString());
                            }
                            break;

                        case ReadState.WaitingForStart2:
                            if (b == START2)
                            {
                                state = ReadState.WaitingForMsb;
                            }
                            else
                            {
                                // False start, go back
                                state = ReadState.WaitingForStart1;
                                DebugOutput?.Invoke(this, ((char)START1).ToString());
                                DebugOutput?.Invoke(this, ((char)b).ToString());
                            }
                            break;

                        case ReadState.WaitingForMsb:
                            expectedLength = b << 8;
                            state = ReadState.WaitingForLsb;
                            break;

                        case ReadState.WaitingForLsb:
                            expectedLength |= b;
                            if (expectedLength > MAX_PACKET_SIZE)
                            {
                                // Corrupted packet, reset
                                state = ReadState.WaitingForStart1;
                                expectedLength = 0;
                            }
                            else if (expectedLength == 0)
                            {
                                // Empty packet
                                state = ReadState.WaitingForStart1;
                            }
                            else
                            {
                                buffer.Clear();
                                state = ReadState.ReadingPayload;
                            }
                            break;

                        case ReadState.ReadingPayload:
                            buffer.Add((byte)b);
                            if (buffer.Count >= expectedLength)
                            {
                                // Complete packet received
                                ProcessPacket(buffer.ToArray());
                                buffer.Clear();
                                state = ReadState.WaitingForStart1;
                                expectedLength = 0;
                            }
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    await Task.Delay(100, ct);
                }
            }
        }

        private void ProcessPacket(byte[] data)
        {
            try
            {
                var fromRadio = FromRadio.FromBytes(data);
                PacketReceived?.Invoke(this, new FromRadioEventArgs(fromRadio));
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new Exception($"Failed to parse FromRadio packet: {ex.Message}", ex));
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        private enum ReadState
        {
            WaitingForStart1,
            WaitingForStart2,
            WaitingForMsb,
            WaitingForLsb,
            ReadingPayload
        }
    }

    public class FromRadioEventArgs : EventArgs
    {
        public FromRadio Packet { get; }
        public FromRadioEventArgs(FromRadio packet) => Packet = packet;
    }
}
