using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace GameServer.Packet
{
    class LoginServerRecv
    {
        public enum RECV_HEADER
        {
            INIT = 0x01,
            USER_LOGIN = 0x02
        }

        private static Dictionary<byte, RecvPacketHandler> packetHandlers;

        static LoginServerRecv()
        {
            packetHandlers = new Dictionary<byte, RecvPacketHandler>();
            Register((byte)RECV_HEADER.INIT, Init);
            Register((byte)RECV_HEADER.USER_LOGIN, UserLogin);
        }

        private static void Register(byte packetID, OnPacketReceive receiveMethod)
        {
            packetHandlers.Add(packetID, new RecvPacketHandler(packetID, receiveMethod));
        }

        public static RecvPacketHandler GetHandler(byte packetID)
        {
            RecvPacketHandler pHandler = null;
            try
            {
                pHandler = packetHandlers[packetID];
            }
            catch (Exception)
            {
                Output.WriteLine("LoginServerRecv::GetHandler Couldn't find a packet handler for packet with ID: " + packetID.ToString());
            }
            return pHandler;
        }

        private static void Init(Connection pConn, byte[] data)
        {
            UInt16 realL = BitConverter.ToUInt16(data, 0);
            if (Program.DEBUG_Login_Recv) Output.WriteLine("LoginServerRecv::Init");
            if (pConn.client.Status != Client.STATUS.Connected)
            {
                if (Program.DEBUG_Login_Recv) Output.WriteLine("LoginServerRecv::Init - STATUS != CONNECTED, close connection");
                pConn.Close();
                return;
            }
            if (realL != 7680)//to get real length need to swap bytes
            {
                if (Program.DEBUG_Login_Recv) Output.WriteLine("LoginServerRecv::Init - Wrong packet size, close connection");
                pConn.Close();
                return;
            }
            if (data[Program.receivePrefixLength + 4] != 0xC1 || data[Program.receivePrefixLength + 7] != 0x12 || data[Program.receivePrefixLength + 10] != 0x54 || data[Program.receivePrefixLength + 12] != 0xC1)
            {
                if (Program.DEBUG_Login_Recv) Output.WriteLine("LoginServerRecv::Init - Wrong packet data, close connection");
                pConn.Close();
                return;
            }
            pConn.StopCheckForInactivity();//stop inactive timer as we are sure that connect right login server
            pConn.client.Status = Client.STATUS.Login;
            Output.WriteLine("LoginServerRecv::Init Login server succesfull connected" + " from endpoint = " + IPAddress.Parse(((IPEndPoint)pConn.AcceptSocket.AcceptSocket.RemoteEndPoint).Address.ToString()) + ": " + ((IPEndPoint)pConn.AcceptSocket.AcceptSocket.RemoteEndPoint).Port.ToString());
            pConn.SendSync(new LoginServerSend.ServerInfo(GameServer.world.WorldMinX, GameServer.world.WorldMinY, GameServer.world.WorldMaxX, GameServer.world.WorldMaxY, Program.port, Program.userIP, Program.userPort));
            int[] curInGameUsersUidList = GameServer.world.GetCurrentUidList();
            pConn.SendSync(new LoginServerSend.BeginUserList());
            if (curInGameUsersUidList.Length > 200)
            {
                int offset = 0;
                int countElements = 0;
                while (true)
                {
                    int[] subList;
                    if (curInGameUsersUidList.Length - countElements >= 200)
                    {
                        subList = new int[200];
                        Array.Copy(curInGameUsersUidList, offset, subList, 0, 200);
                        countElements += 200;
                        offset += 200;
                    }
                    else
                    {
                        if (curInGameUsersUidList.Length - countElements > 0)
                        {
                            subList = new int[curInGameUsersUidList.Length - countElements];
                            Array.Copy(curInGameUsersUidList, offset, subList, 0, curInGameUsersUidList.Length - countElements);
                            countElements += curInGameUsersUidList.Length - countElements;
                            offset += curInGameUsersUidList.Length - countElements;
                        }
                        else
                        {
                            break;
                        }
                    }
                    pConn.SendSync(new LoginServerSend.UserList(subList));
                }
            }
            else
            {
                if(curInGameUsersUidList.Length > 0) pConn.SendSync(new LoginServerSend.UserList(curInGameUsersUidList));
            }
            pConn.SendSync(new LoginServerSend.EndUserList());
            return;
        }

        private static void UserLogin(Connection pConn, byte[] data)
        {
            UInt16 realL = BitConverter.ToUInt16(data, 0);
            if (Program.DEBUG_Login_Recv) Output.WriteLine("LoginServerRecv::UserLogin");
            if (pConn.client.Status != Client.STATUS.Login)
            {
                if (Program.DEBUG_Login_Recv) Output.WriteLine("LoginServerRecv::UserLogin - STATUS != CONNECTED, close connection");
                pConn.Close();
                return;
            }
            if(data.Length < Program.receivePrefixLength + 1 + 4 + 4 + 1 + 16)
            {
                if (Program.DEBUG_Login_Recv) Output.WriteLine("LoginServerRecv::UserLogin - wrong packet size, close connection");
                pConn.Close();
                return;
            }
            int uid = BitConverter.ToInt32(data, Program.receivePrefixLength + 1);
            int pid = BitConverter.ToInt32(data, Program.receivePrefixLength + 1 + 4);
            byte key = data[Program.receivePrefixLength + 1 + 4 + 4];
            byte[] guid = new byte[16];
            Array.Copy(data, Program.receivePrefixLength + 1 + 4 + 4 + 1, guid, 0 , 16);
            /////////// DEBUG ONLY
            //Output.WriteLine("LoginServerRecv::UserLogin FOR DEBUG ONLY CLIENT DATA IS RESEND TO LOGIN SERVER");
            //pConn.Send(new LoginServerSend.UserInGame(uid));
            ///////////
            if(!UsersLobby.Add(uid, pid, key, guid))
            {
                if (Program.DEBUG_Login_Recv) Output.WriteLine("LoginServerRecv::UserLogin - Error adding user login");
                //pConn.Close();
                return;
            }
        }

    }
}
