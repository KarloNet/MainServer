using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace GameServer
{
    class Connection
    {
        //just for assigning an ID so we can watch our objects while testing.
        static int nextTokenId = 0;
        object locker;
        int tokenID;
        TimeSpan lastActiveTime;
        int errorCount;
        SocketAsyncEventArgs sendSocket;
        SocketAsyncEventArgs recvSocket;
        SocketAsyncEventArgs acceptSocket;
        private bool sendDone;
        Queue<Packet.SendPacketHandlers.Packet> sendQueue;
        public bool IsPacketWaitingToSend { get { if (sendQueue.Count > 0) return true; else return false; } }
        public static readonly int MAX_SEND_QUEUE_SIZE = 50000;
        public Client client;

        public Int32 currentRecvBufferPos;
        public Int32 headerBytesReadCount;
        public Byte[] header;
        public Byte[] msg;
        public Int32 incMsgLength;
        public Int32 currMsgBytesRead;

        private Timer liveTimer;
        bool noDelayConnection;
        int maxWaitTime;

        //Set initial state
        public Connection()
        {
            sendSocket = new SocketAsyncEventArgs();
            recvSocket = new SocketAsyncEventArgs();
            acceptSocket = new SocketAsyncEventArgs();
            client = new Client(Program.hashClientKey, this);
            locker = new object();
            lastActiveTime = new TimeSpan(0);
            errorCount = 0;
            tokenID = AssignTokenId();
            currentRecvBufferPos = 0;
            //liveTimer = new Timer(new TimerCallback(TimerCallback), stateObject, dueTimeStart, timeInterval);
            noDelayConnection = false;
            maxWaitTime = 300;//default 5 minutes
            sendDone = true;
            sendQueue = new Queue<Packet.SendPacketHandlers.Packet>();
        }

        public void ReInit(bool checkForActivConnection, int maxInactiveTime)
        {
            locker = new object();
            client = new Client(Program.hashClientKey, this);
            lastActiveTime = new TimeSpan(0);
            errorCount = 0;
            tokenID = AssignTokenId();
            currentRecvBufferPos = 0;
            this.lastActiveTime = DateTime.Now.TimeOfDay;
            this.noDelayConnection = checkForActivConnection;
            this.maxWaitTime = maxInactiveTime;
            sendDone = true;
            sendQueue = new Queue<Packet.SendPacketHandlers.Packet>();
            if (noDelayConnection)
            {
                //in worst case connection can stay untouched almost as long as maxInactiveTime * 2, so to take this down we call check twice as fast as we realy want
                liveTimer = new Timer(new TimerCallback(TimerCallback), null, maxInactiveTime * 1000, (maxInactiveTime / 2) * 1000);
            }
        }

        private void TimerCallback(object stateObject)
        {
            if (noDelayConnection)
            {
                TimeSpan dif;
                dif = DateTime.Now.TimeOfDay - LastActivTime;
                if (dif.TotalSeconds > maxWaitTime)
                {
                    try
                    {
                        Output.WriteLine( ConsoleColor.Yellow, "Connection::TimerCallback Close inactive connections with: " + RecvSocket.AcceptSocket.RemoteEndPoint.ToString());
                        Close();
                    }
                    catch (ObjectDisposedException)
                    {
                        liveTimer.Dispose();
                    }
                }
            }
        }

        public void StopCheckForInactivity()
        {
            if (liveTimer != null)
            {
                liveTimer.Dispose();
            }
        }

        public void StartCheckForInactivity(int maxInactiveTime)
        {
            if (liveTimer != null)
            {
                liveTimer.Dispose();
            }
            liveTimer = new Timer(new TimerCallback(TimerCallback), null, maxInactiveTime * 1000, (maxInactiveTime / 2) * 1000);
        }


        public TimeSpan LastActivTime
        {
            get
            {
                TimeSpan tmp;
                lock (this.locker)
                {
                    tmp = this.lastActiveTime;
                }
                return tmp;
            }
            set
            {
                lock (this.locker)
                {
                    this.lastActiveTime = value;
                }
            }
        }

        public void ConnectionAlive()
        {
            lock (this.locker)
            {
                this.lastActiveTime = DateTime.Now.TimeOfDay;
            }
        }

        internal SocketAsyncEventArgs AcceptSocket
        {
            get
            {
                return acceptSocket;
            }
        }

        internal SocketAsyncEventArgs RecvSocket
        {
            get
            {
                return recvSocket;
            }
        }


        internal SocketAsyncEventArgs SendSocket
        {
            get
            {
                return sendSocket;
            }
        }

        public int AssignTokenId()
        {
            tokenID = Interlocked.Increment(ref nextTokenId);
            return tokenID;
        }

        internal int TokenID
        {
            get
            {
                return tokenID;
            }
        }

        internal string ConnectionIP
        {
            get
            {
                return ((System.Net.IPEndPoint)acceptSocket.AcceptSocket.RemoteEndPoint).Address.ToString();
            }
        }

        //set connection ready for recv and send operations ( acceptSocked was succesfully accepted )
        public void PrepareForRecvSend()
        {
            if (acceptSocket.AcceptSocket != null)
            {
                recvSocket.AcceptSocket = acceptSocket.AcceptSocket;
                recvSocket.UserToken = this;
                sendSocket.AcceptSocket = acceptSocket.AcceptSocket;
                sendSocket.UserToken = this;
            }
        }

        //accept connection
        public void Accept()
        {

        }

        //recv from connection
        public int Recv(Int32 remainingBytesToProcess)
        {
            currentRecvBufferPos = 0;//set initial position in recv buffer to start
            while (remainingBytesToProcess > 0)
            {
                //If we have not got all of the prefix already, then we need to work on it here.                                
                if (headerBytesReadCount < Program.receivePrefixLength)
                {
                    remainingBytesToProcess = Packet.Header.ProcessPrefix(this, remainingBytesToProcess);
                }
                else // we have all header bytes so can start to read message
                {
                    // If we have processed the prefix, we can work on the message now. We'll arrive here when we have received enough bytes to read the first byte after the prefix.
                    remainingBytesToProcess = Packet.Data.ProcessMessage(this, remainingBytesToProcess);
                }
            }
            return remainingBytesToProcess;
        }

        public void ProcessData(byte[] data)
        {
            Packet.RecvPacketHandler handler = Packet.RecvPacketHandlers.GetHandler(data[2]);
            if (handler != null)
            {
                Packet.OnPacketReceive pHandlerMethod = handler.OnReceive;
                try
                {
                    pHandlerMethod(this, data);
                    ConnectionAlive();
                }
                catch (Exception e)
                {
                    Output.WriteLine("Connection::ProcessData - catch exception: " + e.ToString());
                    Close();
                }
                //set new time for last recved packet
                //LastRecv = DateTime.UtcNow.TimeOfDay;
            }
            else
            {
                Output.WriteLine("Connection::ProcessData " + "Wrong packet - close connection");
                Close();
            }
        }

        public void SendSync(Packet.SendPacketHandlers.Packet p)
        {
            lock (locker)
            {
                int packetLength = 0;
                byte[] sendBuffer = null;
                switch (client.EncodeType)
                {
                    case Client.ENCODE_TYPE.AES:
                        break;
                    case Client.ENCODE_TYPE.BXO:
                        break;
                    case Client.ENCODE_TYPE.XOR:
                        if (Program.DEBUG_send) Output.WriteLine("Connection::SendPacket Encoded with XOR and key: " + client.PrivateKey[client.SendKeyOffset].ToString());
                        sendBuffer = p.Compile(client.PrivateKey, client.SendKeyOffset, Client.ENCODE_TYPE.XOR, out packetLength);
                        client.SendKeyOffset++;
                        if (client.SendKeyOffset >= client.PrivateKey.Length) client.SendKeyOffset = 0;
                        break;
                    case Client.ENCODE_TYPE.COD:
                        if (Program.DEBUG_send) Output.WriteLine("Connection::SendPacket" + "PID: " + client.PlayerID.ToString() + " Encode with COD and key: " + client.sendKeyCOD.ToString());
                        byte[] enKey = new byte[1];
                        enKey[0] = client.sendKeyCOD;
                        sendBuffer = p.Compile(enKey, 0, Client.ENCODE_TYPE.COD, out packetLength);
                        client.sendKeyCOD++;
                        if (client.sendKeyCOD >= 63) client.sendKeyCOD = 0;
                        break;
                }

                if (Program.DEBUG_send || Program.DEBUG_Login_Send)
                {
                    string text;
                    if (p.PacketType == Packet.LoginServerSend.SERVER_EXTENDED_PACKET_TYPE) text = String.Format("Send EXTENDED packet type: 0x{0:X2} Length: {1} and after crypt: {2}", p.PacketExtendType, p.PacketLength, packetLength);
                    else text = String.Format("PID: " + client.PlayerID.ToString() + " Send packet type: 0x{0:x2} Length: {1} and after crypt: {2}", p.PacketType, p.PacketLength, packetLength);
                    //Output.WriteLine("Connection::Send " + text);
                }
                //sendQueue.Enqueue(sendBuffer);
                //connectedSocket.BeginSend(sendBuffer, 0, packetLength, 0, new AsyncCallback(SendCallback), connectedSocket);
                //using blocking send mayby async will be bether??
                try
                {
                    int iResult = sendSocket.AcceptSocket.Send(sendBuffer, 0, packetLength, 0);
                    if (iResult == (int)SocketError.SocketError)
                    {
                        Output.WriteLine("Connection::Send -  Send failed with error: " + iResult.ToString());
                    }
                }
                catch (ObjectDisposedException e)
                {
                    Output.WriteLine("Connection::Send -  Send failed with error: " + e.ToString());
                }
                catch (SocketException es)
                {
                    Output.WriteLine("Connection::Send -  Send failed with error: " + es.ToString());
                }
                catch (NullReferenceException en)
                {
                    Output.WriteLine("Connection::Send -  Send failed with error: " + en.ToString());
                }
            }
        }

        //send to connection
        public void SendAsync(Packet.SendPacketHandlers.Packet packet, bool test)
        {
            lock (locker)
            {
                if (sendDone)
                {
                    int curBufCount = 0;
                    Packet.SendPacketHandlers.Packet p = null;
                    byte[] sBuf = null;

                    if(sendQueue.Count > 0)
                    {
                        p = sendQueue.Dequeue();
                        if (packet != null) sendQueue.Enqueue(packet);
                    }
                    else
                    {
                        if (packet != null) p = packet;
                    }
                    if (p == null) return;
                    int packetSize = p.PacketLength + Packet.Encrypt.MAX_ADD_BYTES;
                    if (packetSize > SendSocket.Buffer.Length)
                    {
                        sBuf = encryptPacket(p);
                        sendDone = false;
                        try
                        {
                            sendSocket.AcceptSocket.BeginSend(sBuf, 0, sBuf.Length, 0, new AsyncCallback(SendCallback), this);
                        }
                        catch (Exception e)
                        {
                            Output.WriteLine(e.ToString());
                            Close();
                        }
                    }
                    else
                    {
                        sBuf = encryptPacket(p);
                        Buffer.BlockCopy(sBuf, 0, SendSocket.Buffer, SendSocket.Offset, sBuf.Length);
                        curBufCount += sBuf.Length;
                    }
                }
                else
                {
                    if (packet != null) sendQueue.Enqueue(packet);
                }
            }
        }

        private byte[] encryptPacket(Packet.SendPacketHandlers.Packet p)
        {
            if (p == null) return null;
            int packetLength = 0;
            byte[] sendBuffer = null;
            switch (client.EncodeType)
            {
                case Client.ENCODE_TYPE.AES:
                    break;
                case Client.ENCODE_TYPE.BXO:
                    break;
                case Client.ENCODE_TYPE.XOR:
                    if (Program.DEBUG_send) Output.WriteLine("Connection::SendPacket Encoded with XOR and key: " + client.PrivateKey[client.SendKeyOffset].ToString());
                    sendBuffer = p.Compile(client.PrivateKey, client.SendKeyOffset, Client.ENCODE_TYPE.XOR, out packetLength);
                    client.SendKeyOffset++;
                    if (client.SendKeyOffset >= client.PrivateKey.Length) client.SendKeyOffset = 0;
                    break;
                case Client.ENCODE_TYPE.COD:
                    if (Program.DEBUG_send) Output.WriteLine("Connection::SendPacket" + "PID: " + client.PlayerID.ToString() + " Encode with COD and key: " + client.sendKeyCOD.ToString());
                    byte[] enKey = new byte[1];
                    enKey[0] = client.sendKeyCOD;
                    sendBuffer = p.Compile(enKey, 0, Client.ENCODE_TYPE.COD, out packetLength);
                    client.sendKeyCOD++;
                    if (client.sendKeyCOD >= 63) client.sendKeyCOD = 0;
                    break;
            }
            return sendBuffer;
        }

        //send to connection
        public void SendAsync(Packet.SendPacketHandlers.Packet packet)
        {
            //debug
            if (packet != null && packet.PacketType == (byte)Packet.SendPacketHandlers.SEND_HEADER.SKILL_ANIM)
            {
                Output.WriteLine(ConsoleColor.Green, "Connection::SendAsync Send SKILL_ANIM to: " + RecvSocket.AcceptSocket.RemoteEndPoint.ToString());
            }
            if (packet != null && packet.PacketType == (byte)Packet.SendPacketHandlers.SEND_HEADER.ATTACK_MAG)
            {
                int casterID;
                int targetID;
                int skill_type;
                int skill_level;
                MemoryStream stream = new MemoryStream(packet.PacketData());
                BinaryReader br;
                using (br = new BinaryReader(stream))
                {
                    stream.Position = 0;//set strem position to begin of data (beafore is header data)
                    casterID = br.ReadInt32();
                    targetID = br.ReadInt32();
                    skill_type = br.ReadInt32();
                    skill_level = br.ReadInt32();
                }
                Output.WriteLine(ConsoleColor.Green, "Connection::SendAsync Send ATTACK_MAG to: " + RecvSocket.AcceptSocket.RemoteEndPoint.ToString() + "[" + casterID.ToString() + "->" + targetID.ToString() + "]");
            }
            if(sendQueue.Count > MAX_SEND_QUEUE_SIZE)
            {
                Output.WriteLine( ConsoleColor.Red, "Connection::SendAsync Send queue is full, closing connection: " + RecvSocket.AcceptSocket.RemoteEndPoint.ToString());
                Close();
                return;
            }
            lock (locker)
            {
                if (sendDone)
                {
                    Packet.SendPacketHandlers.Packet p = null;
                    if(sendQueue.Count > 0)
                    {
                        p = sendQueue.Dequeue();
                        if(packet != null) sendQueue.Enqueue(packet);
                    }
                    else
                    {
                        if(packet != null) p = packet;
                    }
                    if (p == null) return;
                    int packetLength = 0;
                    byte[] sendBuffer = null;
                    switch (client.EncodeType)
                    {
                        case Client.ENCODE_TYPE.AES:
                            break;
                        case Client.ENCODE_TYPE.BXO:
                            break;
                        case Client.ENCODE_TYPE.XOR:
                            if (Program.DEBUG_send) Output.WriteLine("Connection::SendPacket Encoded with XOR and key: " + client.PrivateKey[client.SendKeyOffset].ToString());
                            sendBuffer = p.Compile(client.PrivateKey, client.SendKeyOffset, Client.ENCODE_TYPE.XOR, out packetLength);
                            client.SendKeyOffset++;
                            if (client.SendKeyOffset >= client.PrivateKey.Length) client.SendKeyOffset = 0;
                            break;
                        case Client.ENCODE_TYPE.COD:
                            if (Program.DEBUG_send) Output.WriteLine("Connection::SendPacket" + "PID: " + client.PlayerID.ToString() + " Encode with COD and key: " + client.sendKeyCOD.ToString());
                            byte[] enKey = new byte[1];
                            enKey[0] = client.sendKeyCOD;
                            sendBuffer = p.Compile(enKey, 0, Client.ENCODE_TYPE.COD, out packetLength);
                            client.sendKeyCOD++;
                            if (client.sendKeyCOD >= 63) client.sendKeyCOD = 0;
                            break;
                    }
                    if (Program.DEBUG_send || Program.DEBUG_Login_Send)
                    {
                        string text;
                        if (p.PacketType == Packet.LoginServerSend.SERVER_EXTENDED_PACKET_TYPE) text = String.Format("Send EXTENDED packet type: 0x{0:X2} Length: {1} and after crypt: {2}", p.PacketExtendType, p.PacketLength, packetLength);
                        else text = String.Format("PID: " + client.PlayerID.ToString() + " Send packet type: 0x{0:x2} Length: {1} and after crypt: {2}", p.PacketType, p.PacketLength, packetLength);
                        Output.WriteLine("Connection::Send " + text);
                    }
                    sendDone = false;
                    try
                    {
                        sendSocket.AcceptSocket.BeginSend(sendBuffer, 0, sendBuffer.Length, 0, new AsyncCallback(SendCallback), this);
                    }
                    catch ( Exception e)
                    {
                        Output.WriteLine(e.ToString());
                        Close();
                    }
                }
                else
                {
                    if(packet != null) sendQueue.Enqueue(packet);
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Connection sendCon = (Connection)ar.AsyncState;
                Socket client = sendCon.sendSocket.AcceptSocket;
                // Retrieve the socket from the state object.  
                //Socket client = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                if(Program.DEBUG_send)
                    Output.WriteLine("Sent " + bytesSent.ToString() + " bytes to server.");
                lock (sendCon.locker)
                {
                    sendCon.sendDone = true;
                }
                //if(sendCon.IsPacketWaitingToSend) GameServerMainLoop.waitForPacket.Set();
                if (sendCon.IsPacketWaitingToSend) GameServerMainLoop.noSleep = true;
            }
            catch (Exception e)
            {
                Output.WriteLine(e.ToString());
                Connection sendCon = (Connection)ar.AsyncState;
                sendCon.Close();
            }
        }


        //close connection
        public void Close()
        {
            bool shutDownSucces = false;
            //This method closes the socket and releases all resources, both managed and unmanaged. It internally calls Dispose.     
            if (acceptSocket.AcceptSocket != null && acceptSocket.AcceptSocket.Connected)
            {
                acceptSocket.AcceptSocket.Shutdown(SocketShutdown.Both);
                shutDownSucces = true;
                acceptSocket.AcceptSocket.Close();
                acceptSocket.AcceptSocket = null;
            }
            if (recvSocket.AcceptSocket != null)
            {
                if (!shutDownSucces)
                {
                    recvSocket.AcceptSocket.Shutdown(SocketShutdown.Both);
                    shutDownSucces = true;
                }
                recvSocket.AcceptSocket.Close();
                recvSocket.AcceptSocket = null;
            }
            if (sendSocket.AcceptSocket != null)
            {
                if (!shutDownSucces)
                {
                    sendSocket.AcceptSocket.Shutdown(SocketShutdown.Both);
                    shutDownSucces = true;
                }
                sendSocket.AcceptSocket.Close();
                sendSocket.AcceptSocket = null;
            }
            acceptSocket.AcceptSocket = null;
            recvSocket.AcceptSocket = null;
            sendSocket.AcceptSocket = null;
            //remove this user from loged in table
            UsersLobby.Remove(client.UserID, client.PlayerID);
            //remove from in game list
            GameServer.world.RemovePlayer(client.UserID, client.PlayerID);
            //send info to the LoginServer about user logged off
            if(LoginServerInterface.LoginServerConnection != null && client.UserID >= 0)
            {
                LoginServerInterface.LoginServerConnection.SendSync(new Packet.LoginServerSend.UserOutGame(client.UserID));
                client.UserID = -1;
            }
            if (liveTimer != null)
            {
                liveTimer.Dispose();
            }
        }

        public void Dispose()
        {
            if (acceptSocket != null) acceptSocket.Dispose();
            if (recvSocket != null) recvSocket.Dispose();
            if (sendSocket != null) sendSocket.Dispose();
            if (liveTimer != null) liveTimer.Dispose();
        }
    }
}
