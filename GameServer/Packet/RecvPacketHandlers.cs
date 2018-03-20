using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.IO;

namespace GameServer.Packet
{
    class RecvPacketHandlers
    {
        //Login steps
        // 01 - establish connection

        public enum RECV_HEADER
        {
            GameServerLogin = 0x07,
            Move = 0x10,
            MoveStop = 0x11,
            MoveStart = 0x12,
            AttackPhys = 0x13,
            AttackMag = 0x14,
            SkillAnim = 0x15,
            Ping = 0x98,
            Chat = 0x99,
            GAME_SERVER_EXT = 0xFF//only for login server  <-> game server use
        }

        private static Dictionary<byte, RecvPacketHandler> packetHandlers;

        static RecvPacketHandlers()
        {
            packetHandlers = new Dictionary<byte, RecvPacketHandler>();
            Register((byte)RECV_HEADER.GAME_SERVER_EXT, LoginServerExtend);
            Register((byte)RECV_HEADER.GameServerLogin, GameServerLogin);
            Register((byte)RECV_HEADER.Ping, Ping);
            Register((byte)RECV_HEADER.Move, Move);
            Register((byte)RECV_HEADER.MoveStop, MoveStop);
            Register((byte)RECV_HEADER.MoveStart, MoveStart);
            Register((byte)RECV_HEADER.AttackPhys, AttackPhysical);
            Register((byte)RECV_HEADER.AttackMag, AttackMagical);
            Register((byte)RECV_HEADER.SkillAnim, SkillAnim);
            Register((byte)RECV_HEADER.Chat, Chat);
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
                Output.WriteLine("RecvPacketHandlers::GetHandler Couldn't find a packet handler for packet with ID: " + packetID.ToString());
            }
            return pHandler;
        }

        //Process packets for GameServer communication
        public static void LoginServerExtend(Connection pConn, byte[] data)
        {
            if (data.Length <= 5)
            {
                Output.WriteLine("RecvPacketHandlers::LoginServerExtend Wrong packet size");
                pConn.Close();
            }
            Packet.RecvPacketHandler handler = Packet.LoginServerRecv.GetHandler(data[5]);
            if (handler != null)
            {
                Packet.OnPacketReceive pHandlerMethod = handler.OnReceive;
                try
                {
                    pHandlerMethod(pConn, data);
                }
                catch (Exception e)
                {
                    Output.WriteLine("RecvPacketHandlers::LoginServerExtend - catch exception: " + e.ToString());
                    pConn.Close();
                }
                //set new time for last recved packet
                //LastRecv = DateTime.Now.TimeOfDay;
            }
            else
            {
                Output.WriteLine("recvPacketHandlers::LoginServerExtend " + "Wrong packet - close connection");
                pConn.Close();
            }
        }

        public static void Chat(Connection pConn, byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryReader br;
            string text = "";
            string name = "";
            using (br = new BinaryReader(stream))
            {
                stream.Position = Program.receivePrefixLength;//set strem position to begin of data (beafore is header data)
                text = br.ReadString();
                name = br.ReadString();
            }
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Chat Name: " + name + " Text: " + text);
            //GameServer.world.BroadcastPacket(pConn.client.GetPlayer(), new Packet.SendPacketHandlers.Chat(name, text));
            BroadcastPacket bPacket = new BroadcastPacket((uint)pConn.client.GetPlayer().PosX, (uint)pConn.client.GetPlayer().PosY, (int)World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.Chat(name, text));
            GameServer.world.broadcastQueue.Enqueue(bPacket);
        }

        public static void Ping(Connection pConn, byte[] data)
        {
            if(Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Ping");
        }

        public static void Move(Connection pConn, byte[] data)
        {
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Move");
        }

        public static void MoveStop(Connection pConn, byte[] data)
        {
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::MoveStop");
        }

        public static void MoveStart(Connection pConn, byte[] data)
        {
            int id = BitConverter.ToInt32(data, Program.receivePrefixLength);
            int x = BitConverter.ToInt32(data,Program.receivePrefixLength + 4);
            int y = BitConverter.ToInt32(data,Program.receivePrefixLength + 4 + 4);
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::MoveStart");
            pConn.client.GetPlayer().newX = x;
            pConn.client.GetPlayer().newY = y;
            pConn.client.GetPlayer().isCastingSkill = false;
            if (Program.DEBUG_recv)
            {
                Map.Nod myNod = GameServer.world.GetTileAddress((uint)x, (uint)y);
                Output.WriteLine("Recv player start move to [" + x.ToString() + "," + y.ToString() + "] MAP [" + myNod.X.ToString() + "," + myNod.Y.ToString() + "]");
            }
            GameServerMainLoop.noSleep = true;
        }

        public static void AttackPhysical(Connection pConn, byte[] data)
        {
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::AttackPhysical");
            int attackerID = BitConverter.ToInt32(data, Program.receivePrefixLength);// pr.ReadInt32 ();
            int targetID = BitConverter.ToInt32(data, Program.receivePrefixLength + 4);// pr.ReadInt32 ();
            int type = BitConverter.ToInt32(data, Program.receivePrefixLength + 4 + 4); ;// pr.ReadInt32 ();
            int lvl = BitConverter.ToInt32(data, Program.receivePrefixLength + 4 + 4 + 4); ;// pr.ReadInt32 ();
            pConn.client.GetPlayer().isCastingSkill = false;
            //GameServer.world.BroadcastPacket(pConn.client.GetPlayer(), new Packet.SendPacketHandlers.AttackPhy(attackerID, targetID, type, lvl));
            if (pConn.client.GetPlayer().moving) pConn.client.GetPlayer().StopMove();
            BroadcastPacket bPacket = new BroadcastPacket((uint)pConn.client.GetPlayer().PosX, (uint)pConn.client.GetPlayer().PosY, (int)World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.AttackPhy(attackerID, targetID, type, lvl));
            GameServer.world.broadcastQueue.Enqueue(bPacket);
            //mobID = pReader.ReadUInt32();
            //pConn.Player.AttackWithoutSkill((int)mobID);
        }

        public static void AttackMagical(Connection pConn, byte[] data)
        {
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::AttackMagical");
            int casterID = BitConverter.ToInt32(data, Program.receivePrefixLength);// pr.ReadInt32 ();
            int targetID = BitConverter.ToInt32(data, Program.receivePrefixLength + 4);// pr.ReadInt32 ();
            int skill_id = BitConverter.ToInt32(data, Program.receivePrefixLength + 4 + 4); ;// pr.ReadInt32 ();
            int skill_lvl = BitConverter.ToInt32(data, Program.receivePrefixLength + 4 + 4 + 4); ;// pr.ReadInt32 ();
            if (pConn.client.GetPlayer().moving) pConn.client.GetPlayer().StopMove();
            Output.WriteLine(ConsoleColor.DarkRed, "RecvPacketHandlers::AttackMagical SKILL: " + skill_id.ToString());
            //Output.WriteLine( ConsoleColor.DarkRed,"RecvPacketHandlers::AttackMagical DEBUG - SKILL SET TO LighteningBall !");
            //skill_id = 1;
            Skill.OnSkillExecute executeMethod = Skill.SkillHandler.GetSkillExecuteHandler(skill_id, pConn.client.GetPlayer());
            if(executeMethod == null)
            {
                pConn.Close();
                return;
            }
            executeMethod(skill_id, (Database.Player.RACE)pConn.client.GetPlayer().Race, pConn.client.GetPlayer(), targetID);
        }

        public static void SkillAnim(Connection pConn, byte[] data)
        {
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::SkillAnim");
            int casterID = BitConverter.ToInt32(data, Program.receivePrefixLength);// pr.ReadInt32 ();
            int targetID = BitConverter.ToInt32(data, Program.receivePrefixLength + 4);// pr.ReadInt32 ();
            int skill_id = BitConverter.ToInt32(data, Program.receivePrefixLength + 4 + 4); ;// pr.ReadInt32 ();
            int anim_id = BitConverter.ToInt32(data, Program.receivePrefixLength + 4 + 4 + 4); ;// pr.ReadInt32 ();
            if (pConn.client.GetPlayer().moving) pConn.client.GetPlayer().StopMove();
            Output.WriteLine(ConsoleColor.DarkRed, "RecvPacketHandlers::SkillAnim ANIM: " + anim_id.ToString() + " SKILL ID: " + skill_id.ToString());
            //Output.WriteLine(ConsoleColor.DarkRed, "RecvPacketHandlers::SkillAnim DEBUG - SKILL ANIM SET TO LighteningBall !");
            //skill_id = 1;
            Skill.OnSkillRequest requestMethod = Skill.SkillHandler.GetSkillRequestHandler(skill_id, pConn.client.GetPlayer());
            if(requestMethod == null)
            {
                pConn.Close();
                return;
            }
            requestMethod(skill_id, (Database.Player.RACE)pConn.client.GetPlayer().Race, pConn.client.GetPlayer(), targetID);
        }

        public static void GameServerLogin(Connection pConn, byte[] data)
        {
            if (data.Length <= 5)
            {
                Output.WriteLine("RecvPacketHandlers::GameServerLogin Wrong packet size");
                pConn.Close();
            }
            int uid = data[Program.receivePrefixLength];
            int pid = data[Program.receivePrefixLength + 4];
            byte[] guid = new byte[16];
            Array.Copy(data, Program.receivePrefixLength + 4 + 4, guid, 0, 16);
            Output.WriteLine("RecvPacketHandlers::GameServerLogin Recv UID: " + uid.ToString() + " PID: " + pid.ToString() + " GUID: " + GameServer.ByteArrayToHex(guid));
            UsersLobby.LobbyUser uLob = null;
            UsersLobby.Remove(uid, out uLob);
            if (uLob != null)
            {
                //its ok to process check
                if(uid == uLob.UserID && pid == uLob.PlayerID && GameServer.ByteArrayCompare(guid, uLob.GUID))
                {
                    pConn.client.UserID = uid;
                    pConn.client.PlayerID = pid;
                    //client succesfull authenticated
                    Database.Player player = Database.DB_Acces.GetPlayer(pid, pConn);
                    if(player == null)
                    {
                        Output.WriteLine("RecvPacketHandlers::GameServerLogin Not existing player PID: " + pid.ToString());
                        pConn.Close();
                        return;
                    }
                    /*
                    if (!InGameUsers.Add(uid, pid, pConn))
                    {
                        InGameUsers.GameUser gUser = null;
                        InGameUsers.Remove(uid, out gUser);
                        if (gUser != null)
                        {
                            gUser.UserConnection.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.CURRENTLY_LOGGED));
                            gUser.UserConnection.Close();
                        }
                        if (!InGameUsers.Add(uid, pid, pConn))
                        {
                            //shouldn't happen
                            pConn.Close();
                            return;
                        }
                    }
                    */
                    //send info to te Login server about succesfull user login
                    if (LoginServerInterface.LoginServerConnection != null)
                    {
                        LoginServerInterface.LoginServerConnection.SendSync(new Packet.LoginServerSend.UserInGame(pConn.client.UserID));
                    }
                    pConn.client.SetPlayer(player);
                    pConn.client.recvKeyCOD = uLob.StartKey;
                    pConn.client.sendKeyCOD = uLob.StartKey;
                    pConn.client.DecodeType = Client.DECODE_TYPE.COD;
                    pConn.client.EncodeType = Client.ENCODE_TYPE.COD;
                    Output.WriteLine("RecvPacketHandlers::GameServerLogin User succesfully authenticated");
                    pConn.SendAsync(new Packet.SendPacketHandlers.PlayerData());
                    pConn.SendAsync(new Packet.SendPacketHandlers.PlayerSkills());
                    pConn.SendAsync(new Packet.SendPacketHandlers.Inventory());
                    //pConn.Send(new Packet.SendPacketHandlers.PlayerSpawn(pConn));
                    //pConn.client.AddPlayer(player);
                    if (!GameServer.world.AddPlayer(player))
                    {
                        Output.WriteLine("RecvPacketHandlers::GameServerLogin User failed add to map");
                        return;
                    }
                    /*
                    List<Database.Player> pList = GameServer.world.PlayersInSightRange(player.PosX, player.PosY, World.DEBUG_SIGHT_RANGE);
                    List<Database.Mob> mList = GameServer.world.MobsInSightRange(player.PosX, player.PosY, World.DEBUG_SIGHT_RANGE);
                    foreach(Database.Player p in pList)
                    {
                        if (p.PlayerPID != pConn.client.PlayerID)
                        {
                            pConn.client.GetPlayer().AddPlayer(p.PlayerPID);
                            pConn.Send(new Packet.SendPacketHandlers.PlayerSpawn(p.Con));//send p in this point - data about other players to this player
                        }
                    }
                    foreach (Database.Mob m in mList)
                    {
                        pConn.client.GetPlayer().AddMob(m.InternalID);
                        pConn.Send(new Packet.SendPacketHandlers.MobSpawn(m));//send m in this point - data about other mobs to this player
                    }
                    */
                }
                else
                {
                    Output.WriteLine("RecvPacketHandlers::GameServerLogin Client not authenticated by Login server &0x001");
                    Output.WriteLine("RecvPacketHandlers::GameServerLogin Client UID: " + uid.ToString() + " PID: " + pid.ToString() + " GUID: " + GameServer.ByteArrayToHex(guid));
                    Output.WriteLine("RecvPacketHandlers::GameServerLogin From S UID: " + uLob.UserID.ToString() + " PID: " + uLob.PlayerID.ToString() + " GUID: " + GameServer.ByteArrayToHex(uLob.GUID));
                    pConn.Close();
                }
            }
            else
            {
                Output.WriteLine("RecvPacketHandlers::GameServerLogin Client not authenticated by Login server &0x002");
                pConn.Close();
            }
        }
    }
}
