using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using DMSS.CRC;
using System.Threading;

namespace DMSS
{
    [Serializable]
    public class DMSSException : ApplicationException
    {
        public DMSSException(string desc)
            : base(desc)
        {
//            Log.Insert(Log.LogLevels.Error,Log.LogTypes.DMSS,"DMSS Exsception: " + desc);
        }
        public DMSSException(Exception e)
            : base("Receive Thread Exception", e)
        {
//            Log.Insert(Log.LogLevels.Error, Log.LogTypes.DMSS, "DMSS Exsception: " + e.Message);
        }
    }
    public partial class DmssPort
    {
        private object locker= new object();
        private SerialPort port;
        private Queue<byte> RxBuff = new Queue<byte>();
        private EventWaitHandle packetReady = new AutoResetEvent(false);
        private EventWaitHandle packetWait = new AutoResetEvent(false);
        private EventWaitHandle rxDataReady = new AutoResetEvent(false);

        public string GetPortName()
        {
            return new string(port.PortName.ToCharArray());
        }
        public void ChangeBaudRate(int Baud)
        {
//            Log.Insert(Log.LogLevels.LowLevel,Log.LogTypes.DMSS,"BaudRate: " + Baud.ToString());
            lock (locker)
            {
                if (port.IsOpen)
                {
                    PortClose();
                    try
                    {
                        port.BaudRate = Baud;
                        port.Open();
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show("Port Open Error" + ex.Message);
                        throw (new DMSSException("Port Open Error" + ex.Message));
                    }
                }
                else
                {
                    port.BaudRate = Baud;
                }
            }
        }
        public void SetPort(string portN)
        {
//           Log.Insert(Log.LogLevels.LowLevel, Log.LogTypes.DMSS, "SetPort: " + portN.ToString());
            lock (locker)
            {
                if (port.IsOpen)
                {
                    PortClose();
                    try
                    {
                        port.PortName = portN;
                        port.Open();
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show("Port Open Error" + ex.Message);
                        throw (new DMSSException("Port Open Error" + ex.Message));
                    }
                }
                else
                {
                    port.PortName = portN;
                }
            }
        }
        public bool isOpen
        {
            get
            {
                lock (locker)
                {
                    return port.IsOpen;
                }
            }
        }
        public void PortOpen()
        {
            lock (locker)
            {
                PortClose();
                try
                {
                    port.WriteTimeout = 1000;
                    port.Open();
                }
                catch (Exception ex)
                {
                    throw (new DMSSException("Port Open Error" + ex.Message));
                }
            }
        }
        public void PortOpen(string portN)
        {
            lock (locker)
            {
                PortClose();
                try
                {
                    port.PortName = portN;
                    port.WriteTimeout = 1000;
                    port.Open();
                }
                catch (Exception ex)
                {
                    throw (new DMSSException("Port Open Error" + ex.Message));
                }
            }
        }
        public void PortOpen(string portN, int baudeRate)
        {
            lock (locker)
            {
                PortClose();
                try
                {
                    port.PortName = portN;
                    port.BaudRate = baudeRate;
                    port.WriteTimeout = 1000;
                    port.Open();
                }
                catch (Exception ex)
                {
                    throw (new DMSSException("Port Open Error" + ex.Message));
                }
            }
        }
        public void PortOpen(int baudeRate)
        {
            lock (locker)
            {
                PortClose();
                try
                {
                    port.BaudRate = baudeRate;
                    port.WriteTimeout = 1000;
                    port.Open();
                }
                catch (Exception ex)
                {
                    throw (new DMSSException("Port Open Error" + ex.Message));
                }
            }
        }
        public void PortClose()
        {
            lock (locker)
            {
                try
                {
                    if (port.IsOpen)
                    {
                        port.Close();
                    }
                }
                catch (Exception ex)
                {
                    //throw (new DMSSException("Port Close Failure" + ex.Message));
                }
            }
         }
        public DmssPort()
        {
            port = new SerialPort("COM1", 115200, Parity.None, 8, StopBits.One);
            port.DataReceived += new SerialDataReceivedEventHandler(EvDataReceived);
        }
        private void EvDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
                if (e.EventType != SerialData.Chars) return;
                if (port.IsOpen == false) return;
                byte[] data = new byte[port.BytesToRead];
                port.Read(data, 0, data.Length);
                lock (locker)
                {
                    data.ToList().ForEach(b => RxBuff.Enqueue(b));
//                    Log.Insert(Log.LogLevels.LowLevel, Log.LogTypes.DMSS, "<< " + Log.HexDump(data));
                    rxDataReady.Set();
                    if (RxBuff.Contains(0x7E))
                    {
                        packetReady.Set();
                    }
                }
        }
        private List<byte> DecodePacket(List<byte> rx)
        {
            List<byte> pack = new List<byte>();
            bool EscapeChr = false;
            rx.ForEach(b =>
            {
                if (b == 0x7e)
                {
                }
                else if (b == 0x7d)
                {
                    EscapeChr = true;
                }
                else if (EscapeChr == true)
                {
                    pack.Add((byte)(b ^ 0x20));
                    EscapeChr = false;
                }
                else
                {
                    pack.Add(b);
                }
            });
            return pack;
        }
        public void RxClear()
        {
            lock (locker)
            {
                RxBuff.Clear();
            }
        }
        public List<byte> GetResponse(int ms,bool throwError=true)
        {
            List<byte> tmpBuff = new List<byte>();
            
            if (!packetReady.WaitOne(ms, false))
            {
                if (throwError == true)
                    throw (new DMSSException("ReceiveTimeout"));
                else
                    return tmpBuff;
            }
            lock (locker)
            {
                while (RxBuff.Count > 0)
                {
                    byte tmp = RxBuff.Dequeue();
                    if (tmp == 0x7e)
                        break;
                    tmpBuff.Add(tmp);
                }
            }
            if (tmpBuff.Count == 0)
            {
                throw (new DMSSException("ReceiveTimeout NoData"));
            }
            tmpBuff = DecodePacket(tmpBuff);
            if (!Crc16.TestChecksum(tmpBuff.ToArray()))
            {
                throw new DMSSException("CRC Error");
            }
            if (tmpBuff[0] == 0x13) throw (new DMSSException("BadCommand"));
            if (tmpBuff[0] == 0x14) throw (new DMSSException("Bad Parameters Response"));
            if (tmpBuff[0] == 0x15) throw (new DMSSException("Bad Length Response"));
            if (tmpBuff[0] == 0x18) throw (new DMSSException("Bad Mode Response"));
            if (tmpBuff[0] == 0x47) throw (new DMSSException("Bad Security mode"));
            if (tmpBuff[0] == 0x79) 
            {
                //diagnostic messages
                return GetResponse(ms,false);
            }
            tmpBuff.RemoveRange(tmpBuff.Count - 2, 2);
            return tmpBuff;
        }
        private static bool ContainsSequence(byte[] toSearch, byte[] toFind)
        {
            for (var i = 0; i + toFind.Length < toSearch.Length; i++)
            {
                var allSame = true;
                for (var j = 0; j < toFind.Length; j++)
                {
                    if (toSearch[i + j] != toFind[j])
                    {
                        allSame = false;
                        break;
                    }
                }

                if (allSame)
                {
                    return true;
                }
            }

            return false;
        }
        public List<byte> GetData(int ms, byte[] data)
        {
            List<byte> tmpBuff = new List<byte>();
            while (true)
            {
                if (!rxDataReady.WaitOne(ms, false))
                {
                    throw (new DMSSException("ReceiveTimeout"));
                }
                lock (locker)
                {
                    while (RxBuff.Count > 0)
                    {
                        byte tmp = RxBuff.Dequeue();
                        tmpBuff.Add(tmp);
                        if (ContainsSequence(tmpBuff.ToArray(), data))
                        {
                            return tmpBuff;
                        }
                    }
                }
            }
        }
        public List<byte> GetData(int ms)
        {
            List<byte> tmpBuff = new List<byte>();
            if (!packetReady.WaitOne(ms, false))
            {
                throw (new DMSSException("ReceiveTimeout"));
            }

            lock (locker)
            {
                while (RxBuff.Count > 0)
                {
                    byte tmp = RxBuff.Dequeue();
                    tmpBuff.Add(tmp);
                }
            }
            return tmpBuff;
        }
        public void SendBytes(byte[] tosend)
        {
            SendBytes(tosend, 0, tosend.Length);
        }
        public void SendBytes(byte[] tosend, int start, int len)
        {
            try
            {
                port.Write(tosend, start, len);
//                Log.Insert(Log.LogLevels.LowLevel, Log.LogTypes.DMSS, ">> " + Log.HexDump(tosend));
            }
            catch (Exception ex)
            {
                throw (new DMSSException("Port Send Failure" + ex.Message));
            }
        }
        public void SendCommand(byte[] tosend)
        {
            List<byte> TmpBuf = new List<byte>();

            byte[] crc = Crc16.ComputeChecksumBytes(tosend);
            for (int i = 0; i < tosend.Length; i++)
            {
                if (tosend[i] == 0x7e || tosend[i] == 0x7d)
                {
                    TmpBuf.Add(0x7d);
                    TmpBuf.Add((byte)(tosend[i] ^ 0x20));
                }
                else
                {
                    TmpBuf.Add(tosend[i]);
                }
            }
            for (int i = 0; i < 2; i++)
            {
                if (crc[i] == 0x7e || crc[i] == 0x7d)
                {
                    TmpBuf.Add(0x7d);
                    TmpBuf.Add((byte)(crc[i] ^ 0x20));
                }
                else
                {
                    TmpBuf.Add(crc[i]);
                }
            }
            TmpBuf.Add(0x7e);
            RxClear();
            SendBytes(TmpBuf.ToArray());
        }
    }
}
