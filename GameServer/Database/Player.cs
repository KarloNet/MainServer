using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.AI;
using System.Diagnostics;

namespace GameServer.Database
{
    class Player : Entity, AI.Ism
    {
        public enum RACE
        {
            KNIGHT = 0,
            MAGE = 1,
            ARCHER = 2
        }

        int UID;
        int PID;

        string Name;
        int Face;
        int Hair;
        int Exp;
        int Rage;
        int hArmor;
        int gArmor;
        int cArmor;
        int sArmor;
        int bArmor;
        int lHand;
        int rHand;

        object locker = new object();

        Connection con;

        //for skills
        //list of player skills
        public Dictionary<int, Skill.ActiveSkill> activeSkillList;
        public int currentCastingSkillID = 0;
        public bool isCastingSkill = false;

        //for move server side
        public GameServerMainLoop.Vector3 moveTarget;
        public float directionX = 0;
        public float directionY = 0;
        public float mX = 0;
        public float mY = 0;
        public float distance = 0;
        public float speed = 5f;
        public int tmX = 0;
        public int tmY = 0;
        public float startX = 0;
        public float startY = 0;
        public float tstartX = 0;
        public float tstartY = 0;
        public bool moving = false;
        Stopwatch sw = new Stopwatch();

        //
        //public int sightRange = 5;

        private List<int> knownPlayers = new List<int>();
        private List<int> knownMobs = new List<int>();

        public int PlayerUID { get { return this.UID; } set { this.UID = value; } }
        public int PlayerPID { get { return this.PID; } set { this.PID = value; } }
        public string PlayerName { get { return this.Name; } set { this.Name = value; } }
        public int FaceType { get { return this.Face; } set { this.Face = value; } }
        public int HairType { get { return this.Hair; } set { this.Hair = value; } }
        public int Experience { get { return this.Exp; } set { this.Exp = value; } }
        public int ActRage { get { return this.Rage; } set { this.Rage = value; } }
        public int HeadArmor { get { return this.hArmor; } set { this.hArmor = value; } }
        public int GlovesArmor { get { return this.gArmor; } set { this.gArmor = value; } }
        public int ChestArmor { get { return this.cArmor; } set { this.cArmor = value; } }
        public int ShortsArmor { get { return this.sArmor; } set { this.sArmor = value; } }
        public int BootsArmor { get { return this.bArmor; } set { this.bArmor = value; } }
        public int LeftHand { get { return this.lHand; } set { this.lHand = value; } }
        public int RightHand { get { return this.rHand; } set { this.rHand = value; } }
        public Connection Con { get { return con; } }

        public Player(Connection connection) : base(null, null, 0, connection.client.PlayerID)
        {
            con = connection;
            PlayerPID = con.client.PlayerID;
            moveTarget = new GameServerMainLoop.Vector3();
        }

        public Player(Connection connection, Map.Nod startNod) : base (startNod, null, 0, connection.client.PlayerID)
        {
            con = connection;
            PlayerPID = con.client.PlayerID;
            moveTarget = new GameServerMainLoop.Vector3();
        }

        public List<int> KnownPlayers()
        {
            List<int> copy = knownPlayers.ToList();
            return copy;
        }

        public void AddPlayer(int playerID)
        {
            lock (locker)
            {
                knownPlayers.Add(playerID);
            }
        }

        public void RemovePlayer(int playerID)
        {
            lock (locker)
            {
                knownPlayers.Remove(playerID);
            }
        }

        public bool ContainsPlayer(int playerID)
        {
            bool contains = false;
            lock (locker)
            {
                contains = knownPlayers.Contains(playerID);
            }
            return contains;
        }

        public List<int> KnownMobs()
        {
            List<int> copy = knownMobs.ToList();
            return copy;
        }

        public void AddMob(int mobID)
        {
            lock (locker)
            {
                knownMobs.Add(mobID);
            }
        }

        public void RemoveMob(int mobID)
        {
            lock (locker)
            {
                knownMobs.Remove(mobID);
            }
        }

        public bool ContainsMob(int mobID)
        {
            bool contains = false;
            lock (locker)
            {
                contains = knownMobs.Contains(mobID);
            }
            return contains;
        }

        private void Goto()
        {
            float elapsed = sw.ElapsedMilliseconds / 1000f;
            sw.Restart();
            if (Con != null && newX != 0 && newY != 0)
            {
                if ((newX != PosX) || (newY != PosY))
                {
                    if (moveTarget.X != newX || moveTarget.Y != newY)
                    {
                        moveTarget.X = newX;
                        moveTarget.Y = newY;
                        // On starting movement
                        //Output.WriteLine("MOVING TO : [" + newX.ToString() + "," + newY.ToString() + ",0]");
                        distance = (float)(Math.Sqrt(Math.Pow(newX - PosX, 2) + Math.Pow(newY - PosY, 2)));
                        //Output.WriteLine("Distance: " + distance.ToString());
                        directionX = (float)(newX - PosX) / distance;
                        directionY = (float)(newY - PosY) / distance;
                        mX = PosX;
                        mY = PosY;
                        startX = mX;
                        startY = mY;
                        tstartX = mX;
                        tstartY = mY;
                        moving = true;
                        //pl.Con.Send(new Packet.SendPacketHandlers.MoveStart(pl.PlayerPID, pl.newX, pl.newY));// send to client info that server start moving avatar
                        //con.Player.StartMove(con.newX, con.newY, 0);// -> send to all info about move start...
                        //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MoveStart(PlayerPID, newX, newY));
                        state = new AI.EntityState.StartMove(PosX, PosY, new Packet.SendPacketHandlers.MoveStart(PlayerPID, newX, newY));
                        sw.Restart();
                        elapsed = 0;
                        return;
                    }
                    // On update
                    if (moving)
                    {
                        mX = directionX * speed * elapsed;
                        mY = directionY * speed * elapsed;
                        tstartX += mX;
                        tstartY += mY;
                        tmX = (int)(tstartX * 1000);
                        tmY = (int)(tstartY * 1000);
                        //Output.WriteLine("speed " + speed.ToString() + " elapsed " + elapsed.ToString());
                        //Output.WriteLine("mx,my [" + mX.ToString() + ":" + mY.ToString() + "] tstart [" + tstartX.ToString() + ":" + tstartY.ToString() + "]");
                        float tmpDistance = (float)(Math.Sqrt(Math.Pow(tstartX - startX, 2) + Math.Pow(tstartY - startY, 2)));
                        //Output.WriteLine("tmpDistance: " + tmpDistance.ToString());
                        if (tmpDistance >= distance)
                        {
                            PosX = newX;
                            PosY = newY;
                            moving = false;
                            //pl.Con.Send(new Packet.SendPacketHandlers.MoveStop(pl.PlayerPID, pl.tmX, pl.tmY));//send stop packet to user
                            //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MoveStop(PlayerPID, tmX, tmY));
                            state = new AI.EntityState.StopMove(PosX, PosY, new Packet.SendPacketHandlers.MoveStop(PlayerPID, PosX, PosY));
                            newX = newY = 0;
                            //Output.WriteLine(":1 MOV STOP [" + PosX.ToString() + "," + PosY.ToString() + "]");
                            Map.Nod curNod = GameServer.world.GetTileAddress((uint)PosX, (uint)PosY);
                            if (curNod.X != MapPosition.X || curNod.Y != MapPosition.Y)
                            {
                                //player moved to new map tile
                                MapPosition.X = curNod.X;
                                MapPosition.Y = curNod.Y;
                                GameServer.world.MovePlayerOnMap(this, OldMapPosition, MapPosition);
                                OldMapPosition.X = curNod.X;
                                OldMapPosition.Y = curNod.Y;
                            }
                            sw.Stop();
                        }
                        else
                        {
                            if (PosX == newX && PosY == newY)
                            {
                                moving = false;
                                //pl.Con.Send(new Packet.SendPacketHandlers.MoveStop(pl.PlayerPID, pl.tmX, pl.tmY));
                                //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MoveStop(PlayerPID, tmX, tmY));
                                state = new AI.EntityState.StopMove(PosX, PosY, new Packet.SendPacketHandlers.MoveStop(PlayerPID, PosX, PosY));
                                newX = newY = 0;
                                //Output.WriteLine(":2 MOV STOP : [" + PosX.ToString() + "," + PosY.ToString() + "]");
                                Map.Nod curNod = GameServer.world.GetTileAddress((uint)PosX, (uint)PosY);
                                if (curNod.X != MapPosition.X || curNod.Y != MapPosition.Y)
                                {
                                    //player moved to new map tile
                                    MapPosition.X = curNod.X;
                                    MapPosition.Y = curNod.Y;
                                    GameServer.world.MovePlayerOnMap(this, OldMapPosition, MapPosition);
                                    OldMapPosition.X = curNod.X;
                                    OldMapPosition.Y = curNod.Y;
                                }
                                sw.Stop();
                                return;
                            }
                            //Output.WriteLine("[" + mX.ToString() +"," + mY.ToString() + "] to [" + tstartX.ToString() + "," + tstartY.ToString() + "]");
                            //Con.Send(new Packet.SendPacketHandlers.Move(PlayerPID, tmX, tmY));
                            //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.Move(PlayerPID, tmX, tmY));
                            //state = new AI.EntityState.Move(PosX, PosY, new Packet.SendPacketHandlers.Move(PlayerPID, tmX, tmY));
                            //Output.WriteLine("MOV [" + tmX.ToString() + "," + tmY.ToString() + "]");
                            int bX = (int)tstartX - PosX;
                            int bY = (int)tstartY - PosY;
                            if (bX != 0 || bY != 0)
                            {
                                //con.Player.Move(bX, bY, 0);//update player position in sever
                                //for test we do it here....
                                // update the own position
                                PosX += bX;
                                PosY += bY;
                                PosZ += 0;
                                Map.Nod curNod = GameServer.world.GetTileAddress((uint)PosX, (uint)PosY);
                                if (curNod.X != MapPosition.X || curNod.Y != MapPosition.Y)
                                {
                                    //player moved to new map tile
                                    MapPosition.X = curNod.X;
                                    MapPosition.Y = curNod.Y;
                                    GameServer.world.MovePlayerOnMap(this, OldMapPosition, MapPosition);
                                    OldMapPosition.X = curNod.X;
                                    OldMapPosition.Y = curNod.Y;
                                }
                            }
                        }
                    }
                }
                else
                {
                    moving = false;
                    //pl.Con.Send(new Packet.SendPacketHandlers.MoveStop(pl.PlayerPID, pl.tmX, pl.tmY));
                    //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MoveStop(PlayerPID, tmX, tmY));
                    state = new AI.EntityState.StopMove(PosX, PosY, new Packet.SendPacketHandlers.MoveStop(PlayerPID, PosX, PosY));
                    newX = newY = 0;
                    //Output.WriteLine(":2 MOV STOP : [" + PosX.ToString() + "," + PosY.ToString() + "]");
                    Map.Nod curNod = GameServer.world.GetTileAddress((uint)PosX, (uint)PosY);
                    if (curNod.X != MapPosition.X || curNod.Y != MapPosition.Y)
                    {
                        //player moved to new map tile
                        MapPosition.X = curNod.X;
                        MapPosition.Y = curNod.Y;
                        GameServer.world.MovePlayerOnMap(this, OldMapPosition, MapPosition);
                        OldMapPosition.X = curNod.X;
                        OldMapPosition.Y = curNod.Y;
                    }
                    sw.Stop();
                    return;
                }
            }
        }

        public void StopMove()
        {
            moving = false;
            state = new AI.EntityState.StopMove(PosX, PosY, new Packet.SendPacketHandlers.MoveStop(PlayerPID, PosX, PosY));
            newX = newY = 0;
            Map.Nod curNod = GameServer.world.GetTileAddress((uint)PosX, (uint)PosY);
            if (curNod.X != MapPosition.X || curNod.Y != MapPosition.Y)
            {
                MapPosition.X = curNod.X;
                MapPosition.Y = curNod.Y;
                GameServer.world.MovePlayerOnMap(this, OldMapPosition, MapPosition);
                OldMapPosition.X = curNod.X;
                OldMapPosition.Y = curNod.Y;
            }
            sw.Stop();
        }

        void DispatchInfoToNearPlayers(List<int> playersList, List<int> mobsList)
        {

        }

        void UpdatePlayerWorld(List<int> playersList, List<int> mobsList)
        {
            //send info to all players in sight range about new player spawn
            //List<int> playersList = GameServer.world.GetPlayersAtRange(MapPosition, World.DEBUG_SIGHT_RANGE);
            Database.Player p;
            foreach (int i in playersList)
            {
                p = null;
                GameServer.world.players.TryGetValue(i, out p);
                if (!ContainsPlayer(i))//this is new player in range so send spawn and add to known list
                {
                    if (p != null)
                    {
                        AddPlayer(i);
                        Con.SendAsync(new Packet.SendPacketHandlers.PlayerSpawn(p.Con));//newly added player data!
                        if (p.moving)
                        {
                            Con.SendAsync(new Packet.SendPacketHandlers.MoveStart(p.PlayerPID, p.newX, p.newY));//newly added player data!
                        }
                    }
                }
                if(p != null && p.state != null)
                {
                    p.state.SendState(Con);
                }
            }
            List<int> oldPlayersList = KnownPlayers();
            foreach (int i in oldPlayersList)
            {
                if (!playersList.Contains(i))//this player isn't in range so send despawn
                {
                    RemovePlayer(i);
                    Con.SendAsync(new Packet.SendPacketHandlers.PlayerDespawn(i));//newly added player data!
                }
            }
            //check for new mobs in range
            //List<int> mobsList =  GameServer.world.GetMobsAtRange(MapPosition, World.DEBUG_SIGHT_RANGE);
            Database.Mob m;
            foreach (int i in mobsList)
            {
                m = null;
                GameServer.world.monsters.TryGetValue(i, out m);
                if (!ContainsMob(i))//this is new mob in range so send spawn and add to known list
                {
                    if (m != null)
                    {
                        AddMob(i);
                        //m.AddPlayer(PlayerPID);
                        Con.SendAsync(new Packet.SendPacketHandlers.MobSpawn(m));//newly added player data!
                        if (m.moving)
                        {
                            Con.SendAsync(new Packet.SendPacketHandlers.MobMoveStart(m.InternalID, m.newX, m.newY));//newly added player data!
                        }
                    }
                }
                if (m != null && m.state != null)
                {
                    m.state.SendState(Con);
                }
            }
            List<int> oldMobsList = KnownMobs();
            foreach (int i in oldMobsList)
            {
                if (!mobsList.Contains(i))//this mob isn't in range so send despawn
                {
                    RemoveMob(i);
                    Con.SendAsync(new Packet.SendPacketHandlers.MobDespawn(i));//despawn mob
                }
            }
        }

        void Ism.Wonder()
        {
            throw new NotImplementedException();
        }

        void Ism.Stand()
        {
            throw new NotImplementedException();
        }

        void Ism.Attack()
        {
            throw new NotImplementedException();
        }

        void Ism.Run()
        {
            throw new NotImplementedException();
        }

        void Ism.Update(Ism entityIsm)
        {
            Goto();
        }

        EntityState.STATE Ism.GetCurrentState()
        {
            throw new NotImplementedException();
        }

        void Ism.UpdateFrame(EntityState.State entityState)
        {
            List<int> nearPlayers = GameServer.world.GetPlayersAtRange(MapPosition, World.DEBUG_SIGHT_RANGE);
            List<int> nearMobs = GameServer.world.GetMobsAtRange(MapPosition, World.DEBUG_SIGHT_RANGE);
            UpdatePlayerWorld(nearPlayers, nearMobs);
            DispatchInfoToNearPlayers(nearPlayers, nearMobs);
            //entityState.BroadcastState(PosX, PosY);
        }
    }
}
