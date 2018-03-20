using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace GameServer.Packet
{
    class LoginServerSend
    {
        public enum SEND_HEADER
        {
            BEGIN_USER_LIST = 0x01,
            USER_LIST = 0x02,
            END_USER_LIST = 0x03,
            USER_IN_GAME = 0x04,
            USER_OUT_GAME = 0x05,
            SERVER_INFO = 0x06,
            CHAT = 0x99
        }
        public const byte SERVER_EXTENDED_PACKET_TYPE = 0xFF;

        public sealed class BeginUserList : Packet.SendPacketHandlers.Packet
        {
            public BeginUserList()
                : base((byte)SERVER_EXTENDED_PACKET_TYPE)
            {
                SetCapacity((ushort)(5));
                streamWriter.Write((byte)SEND_HEADER.BEGIN_USER_LIST);
                streamWriter.Write(Program.random.Next());
                streamWriter.Write(Program.random.Next());
                streamWriter.Write(Program.random.Next());
                streamWriter.Write(Program.random.Next());
            }
        }

        public sealed class EndUserList : Packet.SendPacketHandlers.Packet
        {
            public EndUserList()
                : base((byte)SERVER_EXTENDED_PACKET_TYPE)
            {
                SetCapacity((ushort)(5));
                streamWriter.Write((byte)SEND_HEADER.END_USER_LIST);
                streamWriter.Write(Program.random.Next());
                streamWriter.Write(Program.random.Next());
                streamWriter.Write(Program.random.Next());
                streamWriter.Write(Program.random.Next());
            }
        }

        public sealed class UserList : Packet.SendPacketHandlers.Packet
        {
            public UserList(int[] uidArray)
                : base((byte)SERVER_EXTENDED_PACKET_TYPE)
            {
                SetCapacity((ushort)(uidArray.Length * 4));//its int array not byte one
                streamWriter.Write((byte)SEND_HEADER.USER_LIST);
                for (int i = 0; i < uidArray.Length; i++)
                {
                    int tmp = uidArray[i];
                    streamWriter.Write(tmp);
                }
            }
        }

        public sealed class UserInGame : Packet.SendPacketHandlers.Packet
        {
            public UserInGame(int UID)
                : base((byte)SERVER_EXTENDED_PACKET_TYPE)
            {
                SetCapacity((ushort)(5));
                streamWriter.Write((byte)SEND_HEADER.USER_IN_GAME);
                streamWriter.Write(UID);
            }
        }

        public sealed class UserOutGame : Packet.SendPacketHandlers.Packet
        {
            public UserOutGame(int UID)
                : base((byte)SERVER_EXTENDED_PACKET_TYPE)
            {
                SetCapacity((ushort)(5));
                streamWriter.Write((byte)SEND_HEADER.USER_OUT_GAME);
                streamWriter.Write(UID);
            }
        }

        public sealed class ServerInfo : Packet.SendPacketHandlers.Packet
        {
            public ServerInfo(uint x, uint y, uint xEnd, uint yEnd, int port, string userIP, int userPort)
                : base((byte)SERVER_EXTENDED_PACKET_TYPE, 41)
            {
                streamWriter.Write((byte)SEND_HEADER.SERVER_INFO);
                streamWriter.Write(x);
                streamWriter.Write(y);
                streamWriter.Write(xEnd);
                streamWriter.Write(yEnd);
                streamWriter.Write(port);
                streamWriter.Write(Map.MapData.GRIDSIZE);
                streamWriter.Write(Map.MapData.TILESIZE_X);
                streamWriter.Write(Map.MapData.TILESIZE_Y);
                streamWriter.Write(Map.MapData.X_MULTIPLIKATOR);
                streamWriter.Write(Map.MapData.Y_MULTIPLIKATOR);
                streamWriter.Write(userIP);
                streamWriter.Write(userPort);
            }
        }
    }
}
