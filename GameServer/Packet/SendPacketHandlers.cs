using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GameServer.Packet
{
    class SendPacketHandlers
    {
        static Random random = new Random(DateTime.Now.Millisecond);

        public enum LOGIN_ERROR
        {
            UNDEFINED = 0x01,
            WRONGID = 0x02,
            WRONG_PASS = 0x03,
            CONNECT_LATER = 0x04,
            BLOCKED = 0x05,
            ID_EXPIRED = 0x06,
            TOO_YOUNG = 0x07,
            NOT_ALLOWED = 0x08,
            CURRENTLY_LOGGED = 0x09,
            CONNECT_AGAIN = 0x10
        }

        public enum SEND_HEADER
        {
            PLAYER = 0x04,
            LOGIN_ERROR = 0x02,
            OPERATION_STATUS = 0x07,
            //In game user
            PLAYER_DATA = 0x15,
            PLAYER_SKILLS = 0x16,
            INVENTORY = 0x17,
            PLAYER_SPAWN = 0x18,
            MOB_SPAWN = 0x19,
            PLAYER_DESPAWN = 0x20,
            MOB_DESPAWN = 0x21,
            MOVE = 0x22,
            MOVE_STOP = 0x23,
            MOVE_START = 0x24,
            MOB_MOVE = 0x25,
            MOB_MOVE_STOP = 0x26,
            MOB_MOVE_START = 0x27,
            ATTACK_PHY = 0x28,
            ATTACK_MAG = 0x29,
            SKILL_ANIM = 0x30,
            CHAT = 0x99
        }

        /*public enum OPERATION_TYPE
        {
            CHARACTER_CREATE = 0x01,
            CHARACTER_DELETE = 0x02,
            CHARACTER_SELECT = 0x03,
        }*/

        public enum OPERATION_STATUS
        {
            SUCCES = 0x01,
            FAIL = 0x02
        }

        public abstract class Packet
        {
            protected MemoryStream memStream;
            protected BinaryWriter streamWriter;
            private ushort packetLength;
            private ushort packetDataLength;
            private byte packetType;
            private byte packetExtendType;
            private bool isCompiled = false;

            public Packet(byte pType, ushort pLength)
            {
                packetType = pType;
                packetExtendType = 0;
                packetDataLength = (ushort)(pLength);
                packetLength = (ushort)(packetDataLength + Program.sendPrefixLength + Program.sendHeaderLength);
                memStream = new MemoryStream((int)packetDataLength);
                streamWriter = new BinaryWriter(memStream);
            }

            public Packet(byte pType)
            {
                packetType = pType;
                packetExtendType = 0;
            }

            public Packet(Packet p)
            {
                packetType = p.packetType;
                packetExtendType = 0;
                packetDataLength = p.packetDataLength;
                packetLength = (ushort)(packetDataLength + Program.sendPrefixLength + Program.sendHeaderLength);
                memStream = new MemoryStream();
                p.memStream.Position = 0;
                p.memStream.CopyTo(memStream);
                streamWriter = new BinaryWriter(memStream);
            }

            public void SetCapacity(ushort newCapacity)
            {
                packetDataLength = (ushort)(newCapacity);
                packetLength = (ushort)(packetDataLength + Program.sendPrefixLength + Program.sendHeaderLength);
                memStream = new MemoryStream(packetDataLength);
                streamWriter = new BinaryWriter(memStream);
            }

            public byte[] Compile(byte[] key, int keyOffset, Client.ENCODE_TYPE encodeType, out int outLength)
            {
                byte[] encryptedPacket;
                packetExtendType = memStream.GetBuffer()[0];
                //if (!isCompiled)
                //{
                    memStream.Position = 0;
                //    streamWriter.Write(Encrypt.NewPacket(memStream.ToArray(), packetType, key, keyOffset, encodeType));
                    encryptedPacket = Encrypt.NewPacket(memStream.ToArray(), packetType, key, keyOffset, encodeType);
                //    isCompiled = true;
                //}
                outLength = (int)encryptedPacket.Length;
                return encryptedPacket;
            }

            public byte[] PacketData()
            {
                return memStream.ToArray();
            }

            public byte PacketExtendType { get { return packetExtendType; } }
            public byte PacketType { get { return this.packetType; } }
            public ushort PacketLength { get { return this.packetLength; } }
            public ushort PacketDataLength { get { return this.packetDataLength; } }
        }

        public sealed class CopyPacket : Packet
        {
            public CopyPacket(Packet p)
                : base(p)
            {
                if (Program.DEBUG_send)
                {
                    string text = String.Format("TYPE: 0x{0:x2}", p.PacketType);
                    Output.WriteLine("SendPacketHandlers::CopyPacket Send " + text);
                }
            }
        }

        public sealed class Player : Packet
        {
            public Player(byte[] data)
                : base((byte)SEND_HEADER.PLAYER)
            {
                SetCapacity((ushort)(data.Length));
                streamWriter.Write(data);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::Player Send");
            }
        }

        public sealed class OperationStatus : Packet
        {
            public OperationStatus(int operationType, int operationStatus)
                : base((byte)SEND_HEADER.OPERATION_STATUS, 12)
            {
                streamWriter.Write((int)operationType);
                streamWriter.Write((int)0);
                streamWriter.Write((int)operationStatus);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::OperationStatus Send");
            }
        }

        public sealed class PlayerData : Packet
        {
            public PlayerData()
                : base((byte)SEND_HEADER.PLAYER_DATA, 12)
            {
                streamWriter.Write(1);
                streamWriter.Write(0);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::PlayerData Send");
            }
        }
        public sealed class PlayerSkills : Packet
        {
            public PlayerSkills()
                : base((byte)SEND_HEADER.PLAYER_SKILLS, 12)
            {
                streamWriter.Write(1);
                streamWriter.Write(0);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::PlayerSkills Send");
            }
        }

        public sealed class Inventory : Packet
        {
            public Inventory()
                : base((byte)SEND_HEADER.INVENTORY, 12)
            {
                streamWriter.Write(1);
                streamWriter.Write(0);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::Inventory Send");
            }
        }

        public sealed class PlayerSpawn : Packet
        {
            public PlayerSpawn(Connection p)
                : base((byte)SEND_HEADER.PLAYER_SPAWN)
            {
                SetCapacity((ushort)(4*6 + p.client.GetPlayer().PlayerName.Length));
                streamWriter.Write(1);
                streamWriter.Write(p.client.PlayerID);
                streamWriter.Write(p.client.GetPlayer().PosX);
                streamWriter.Write(p.client.GetPlayer().PosY);
                streamWriter.Write(p.client.GetPlayer().PlayerName);
                streamWriter.Write(p.client.GetPlayer().Race);
                streamWriter.Write(p.client.GetPlayer().Job);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::PlayerSpawn ID " + p.client.PlayerID.ToString() + " [" + p.client.GetPlayer().PosX.ToString() + "," + p.client.GetPlayer().PosY.ToString() + "]");
            }
        }

        public sealed class MobSpawn : Packet
        {
            public MobSpawn(Database.Mob mob)
                : base((byte)SEND_HEADER.MOB_SPAWN, 29)
            {
                streamWriter.Write(1);
                streamWriter.Write(mob.InternalID);
                streamWriter.Write(mob.PosX);
                streamWriter.Write(mob.PosY);
                streamWriter.Write(mob.Type);
                streamWriter.Write(mob.MaxHealth);
                streamWriter.Write(mob.ActHealth);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::MobSpawn Send [" +  mob.PosX.ToString() + "," + mob.PosY.ToString() + "," + mob.PosZ.ToString() + "]");
            }
        }

        public sealed class PlayerDespawn : Packet
        {
            public PlayerDespawn(int playerID)
                : base((byte)SEND_HEADER.PLAYER_DESPAWN, 12)
            {
                streamWriter.Write(1);
                streamWriter.Write(playerID);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::PlayerDespawn ID " + playerID.ToString());
            }
        }

        public sealed class MobDespawn : Packet
        {
            public MobDespawn(int mobID)
                : base((byte)SEND_HEADER.MOB_DESPAWN, 12)
            {
                streamWriter.Write(1);
                streamWriter.Write(mobID);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::MobDespawn ID " + mobID.ToString());
            }
        }

        public sealed class Move : Packet
        {
            public Move(int playerID, int x, int y)
                : base((byte)SEND_HEADER.MOVE, 20)
            {
                streamWriter.Write(1);
                streamWriter.Write(playerID);
                streamWriter.Write(x);
                streamWriter.Write(y);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::Move ID " + playerID.ToString() + " [" + x.ToString() + "," + y.ToString() + "]");
            }
        }

        public sealed class MoveStop : Packet
        {
            public MoveStop(int playerID, int x, int y)
                : base((byte)SEND_HEADER.MOVE_STOP, 20)
            {
                streamWriter.Write(1);
                streamWriter.Write(playerID);
                streamWriter.Write(x);
                streamWriter.Write(y);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::MoveStop ID " + playerID.ToString() + " [" + x.ToString() + "," + y.ToString() + "]");
            }
        }

        public sealed class MoveStart : Packet
        {
            public MoveStart(int playerID, int x, int y)
                : base((byte)SEND_HEADER.MOVE_START, 20)
            {
                streamWriter.Write(1);
                streamWriter.Write(playerID);
                streamWriter.Write(x);
                streamWriter.Write(y);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::MoveStart ID " + playerID.ToString() + " [" + x.ToString() + "," + y.ToString() + "]");
            }
        }

        public sealed class MobMove : Packet
        {
            public MobMove(int mobID, int x, int y)
                : base((byte)SEND_HEADER.MOB_MOVE, 20)
            {
                streamWriter.Write(1);
                streamWriter.Write(mobID);
                streamWriter.Write(x);
                streamWriter.Write(y);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::MobMove ID " + mobID.ToString() + " [" + x.ToString() + "," + y.ToString() + "]");
            }
        }

        public sealed class MobMoveStop : Packet
        {
            public MobMoveStop(int mobID, int x, int y)
                : base((byte)SEND_HEADER.MOB_MOVE_STOP, 20)
            {
                streamWriter.Write(1);
                streamWriter.Write(mobID);
                streamWriter.Write(x);
                streamWriter.Write(y);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::MobMoveStop ID " + mobID.ToString() + " [" + x.ToString() + "," + y.ToString() + "]");
            }
        }

        public sealed class MobMoveStart : Packet
        {
            public MobMoveStart(int mobID, int x, int y)
                : base((byte)SEND_HEADER.MOB_MOVE_START, 20)
            {
                streamWriter.Write(1);
                streamWriter.Write(mobID);
                streamWriter.Write(x);
                streamWriter.Write(y);
                streamWriter.Write(1);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::MobMoveStart ID " + mobID.ToString() + " [" + x.ToString() + "," + y.ToString() + "]");
            }
        }

        public sealed class AttackPhy : Packet
        {
            public AttackPhy(int attackerID, int targetID, int type, int level)
                : base((byte)SEND_HEADER.ATTACK_PHY, 16)
            {
                streamWriter.Write(attackerID);
                streamWriter.Write(targetID);
                streamWriter.Write(type);
                streamWriter.Write(level);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::AttackPhy TargetID " + targetID.ToString() + " type: " + type.ToString() + " lvl: " + level.ToString());
            }
        }

        public sealed class AttackMag : Packet
        {
            public AttackMag(int casterID, int targetID, int skill_type, int skill_level)
                : base((byte)SEND_HEADER.ATTACK_MAG, 16)
            {
                streamWriter.Write(casterID);
                streamWriter.Write(targetID);
                streamWriter.Write(skill_type);
                streamWriter.Write(skill_level);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::AttackMag TargetID " + targetID.ToString() + " Skill type: " + skill_type.ToString() + " skill lvl: " + skill_level.ToString());
            }
        }

        public sealed class SkillAnim : Packet
        {
            public SkillAnim(int casterID, int targetID, int skill_ID, int anim_ID)
                : base((byte)SEND_HEADER.SKILL_ANIM, 16)
            {
                streamWriter.Write(casterID);
                streamWriter.Write(targetID);
                streamWriter.Write(skill_ID);
                streamWriter.Write(anim_ID);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::SkillAnim TargetID " + targetID.ToString() + " skill ID: " + skill_ID.ToString() + " anim ID: " + anim_ID.ToString());
            }
        }

        public sealed class LoginError : Packet
        {
            public LoginError(LOGIN_ERROR errorNumber)
                : base((byte)SEND_HEADER.LOGIN_ERROR)
            {
                SetCapacity(10);
                for (int i = 0; i < 10; i++)
                {
                    switch (i)
                    {
                        case 3:
                            streamWriter.Write((byte)errorNumber);
                            break;
                        case 6:
                            streamWriter.Write((byte)errorNumber);
                            break;
                        default:
                            streamWriter.Write((byte)random.Next(0, 255));
                            break;
                    }
                }
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::LoginError Send");
            }
        }

        public sealed class Chat : Packet
        {
            public Chat(string chatName, string chatMessage)
                : base((byte)SEND_HEADER.CHAT)
            {
                SetCapacity((ushort)(chatMessage.Length + chatName.Length + 1));
                streamWriter.Write(chatName);
                streamWriter.Write(chatMessage);
                streamWriter.Write((byte)0x00);
                if (Program.DEBUG_send) Output.WriteLine("SendPacketHandlers::Chat Send Name: " + chatName + " Text: " + chatMessage);
            }
        }

    }
}
