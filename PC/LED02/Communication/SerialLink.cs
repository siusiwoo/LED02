using LED02.Packet;
using System.Collections.Concurrent;
using System.IO.Ports;
namespace LED02.Communication
{

    internal class SerialLink
    {
        public Task? rxTask, procTask, StatusTask;
        public bool running = false;
        public bool collecting = false;
        private readonly object sendLock = new object();
        public List<byte> rxBuf = new List<byte>();


        public ConcurrentQueue<byte[]> pktQueue = new ConcurrentQueue<byte[]>();

        public event Action<byte[]> PacketReceived;
        public string lastError = "";
        public SerialPort serialPort;
        public SerialLink()
        {

            serialPort = new SerialPort();


        }
        public bool Connect(string portName, int baudRate)
        {
            try
            {
                Disconnect();

                serialPort.PortName = portName;
                serialPort.BaudRate = baudRate;
                serialPort.Open();

                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                Disconnect();
                running = false;
                return false;
            }
        }
        public void Disconnect()
        {
            if (serialPort != null)
            {
                if (serialPort.IsOpen)
                {
                    running = false;
                    serialPort.Close();
                }
            }
        }

        public void SendPacket(byte[] data, int cmd, int len)
        {
            byte[] pkt = new byte[5 + data.Length];

            lock (sendLock)
            {

                pkt[0] = Protocol.STX;
                pkt[1] = (byte)cmd;
                pkt[2] = (byte)len;
                for (int i = 0; i < data.Length; i++)
                {
                    pkt[3 + i] = data[i];
                }
                pkt[3 + len] = PacketCodec.CalcCrc(pkt, 1, 2 + len);
                pkt[4 + len] = Protocol.ETX;
                serialPort.Write(pkt, 0, pkt.Length);
            }
        }
        public void StartThreads()
        {
            running = true;
            rxTask = Task.Run(ReceiveLoop);
            procTask = Task.Run(ProcessLoop);
            StatusTask = Task.Run(StatusLoop);
        }
        private void ReceiveLoop()
        {
            while (running)
            {
                if (serialPort.BytesToRead == 0)
                {
                    Thread.Sleep(5);
                    continue;
                }
                byte b = (byte)serialPort.ReadByte();

                if (!collecting)
                {
                    if (b == Protocol.STX)
                    {
                        collecting = true;
                        rxBuf.Clear();
                        rxBuf.Add(b);
                    }
                    continue;
                }

                rxBuf.Add(b);

                if (rxBuf.Count >= 5 && rxBuf.Count == rxBuf[2] + 5)//len 데이터 길이 + STX, CMD, LEN, CRC, ETX
                {
                    byte[] pkt = rxBuf.ToArray();
                    pktQueue.Enqueue(pkt);
                    collecting = false;
                    rxBuf.Clear();
                }
                else if (rxBuf.Count >= 32)
                {
                    collecting = false;
                    rxBuf.Clear();
                }


            }

        }

        void ProcessLoop()
        {
            while (running || !pktQueue.IsEmpty)
            {
                if (pktQueue.TryDequeue(out byte[]? pkt))
                {
                    int len = pkt[2];
                    byte etx = pkt[4 + len];
                    byte receivedCrc = pkt[3 + len];
                    byte myCrc = PacketCodec.CalcCrc(pkt, 1, 2 + len);

                    bool endOk = etx == Protocol.ETX;
                    bool crcOk = receivedCrc == myCrc;

                    if (endOk && crcOk)
                    {
                        PacketReceived?.Invoke(pkt);
                    }
                }
                else Thread.Sleep(5);
            }
        }
        void StatusLoop() // 상태
        {
            while (running)
            {
                SendPacket(new byte[0], Protocol.Status_READ, 0);
                Thread.Sleep(10);
            }
        }
    }
}
