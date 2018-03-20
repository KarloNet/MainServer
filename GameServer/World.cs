using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Drawing;

namespace GameServer
{
    class BroadcastPacket
    {
        Packet.SendPacketHandlers.Packet packet;
        uint xPos;
        uint yPos;
        int range;

        public BroadcastPacket(uint xpos, uint ypos, int range, Packet.SendPacketHandlers.Packet p)
        {
            packet = p;
            xPos = xpos;
            yPos = ypos;
            this.range = range;
        }

        public uint X { get { return xPos; } }
        public uint Y { get { return yPos; } }
        public int Range { get { return range; } }
        public Packet.SendPacketHandlers.Packet Packet { get { return packet; } }
    }

    class World
    {
        public static uint DEBUG_SIGHT_RANGE = 10;//--------------------------------------   TEMPORARY VALUE ! -------------------------------

        public ConcurrentDictionary<int, Database.Mob> monsters;
        public ConcurrentDictionary<int, Database.Player> players;
        public ConcurrentQueue<BroadcastPacket> broadcastQueue;
        public ConcurrentQueue<Skill.Action> actions;

        Map.MapData mapa;
        string mLoad = "";

        public World(string mapName)
        {
            mLoad = mapName;
            monsters = new ConcurrentDictionary<int, Database.Mob>();
            players = new ConcurrentDictionary<int, Database.Player>();
            broadcastQueue = new ConcurrentQueue<BroadcastPacket>();
            actions = new ConcurrentQueue<Skill.Action>();
        }

        public bool Init()
        {
            Map.MapLoader ml = new Map.MapLoader();
            mapa = ml.LoadMap(mLoad, this);
            if (mapa == null) return false;
            ml.Init();
            return true;
        }

        public void AddMonster(Database.Mob mb)
        {
            mapa.AddEntityAtTile(mb.MapPosition, mb.InternalID, false);
            monsters.TryAdd(mb.InternalID, mb);
            //BroadcastPacket(mb.PosX, mb.PosY, mb.sightRange, new Packet.SendPacketHandlers.MobSpawn(mb));
            /*
            List<int> playersList = mapa.GetAllPlayersAtRange(mb.MapPosition, DEBUG_SIGHT_RANGE);
            Database.Player p;
            foreach (int i in playersList)
            {
                p = null;
                players.TryGetValue(i, out p);
                if (p != null)
                {
                    mb.AddPlayer(i);
                    p.Con.Send(new Packet.SendPacketHandlers.MobSpawn(mb));//newly added player data!
                }
            }
            */
        }

        public Map.Nod GetPositionAtTile(uint xTile, uint yTile)
        {
            return mapa.GetPositionAtTile(xTile, yTile);
        }

        public Map.Nod GetTileAddress(uint xPos, uint yPos)
        {
            return mapa.GetTileAddress(xPos, yPos);
        }

        public List<int> GetPlayersAtRange(uint x, uint y, uint sightRange)
        {
            return mapa.GetAllPlayersAtRange(x, y, sightRange);
        }

        public List<int> GetPlayersAtRange(Map.Nod position, uint sightRange)
        {
            return mapa.GetAllPlayersAtRange(position, sightRange);
        }

        public List<int> GetMobsAtRange(uint x, uint y, uint sightRange)
        {
            return mapa.GetAllMobsAtRange(x, y, sightRange);
        }

        public List<int> GetMobsAtRange(Map.Nod position, uint sightRange)
        {
            return mapa.GetAllMobsAtRange(position, sightRange);
        }

        public List<Database.Mob> MobsInSightRange(int x, int y, uint sightRange)
        {
            Map.Nod nod = mapa.GetTileAddress((uint)x, (uint)y);
            List<int> mobsList = mapa.GetAllMobsAtRange(nod, DEBUG_SIGHT_RANGE);
            List<Database.Mob> mobsL = new List<Database.Mob>(mobsList.Capacity);
            Database.Mob m;
            foreach (int i in mobsList)
            {
                m = null;
                monsters.TryGetValue(i, out m);
                if (m != null)
                {
                    mobsL.Add(m);
                }
            }
            return mobsL;
        }

        public int GetDistance(int obj1X, int obj1Y, int obj2ID)
        {
            Database.Entity entity;
            entity = GetEntity(obj2ID);
            if (entity == null) return int.MaxValue;
            return GetDistance(obj1X, obj1Y, entity.PosX, entity.PosY);
        }
        public int GetDistance(int obj1X, int obj1Y, int obj2X, int obj2Y)
        {
            int deltaX = (obj1X - obj2X)*(obj1X - obj2X);
            int deltaY = (obj1Y - obj2Y)*(obj1Y - obj2Y);
            int range = Math.Abs(deltaX) + Math.Abs(deltaY);
            return range;
        }

        public int GetDistanceSqr(int obj1X, int obj1Y, int obj2ID)
        {
            return (int)Math.Sqrt(GetDistance(obj1X, obj1Y, obj2ID));
        }
        public int GetDistanceSqr(int obj1X, int obj1Y, int obj2X, int obj2Y)
        {
            return (int)Math.Sqrt(GetDistance(obj1X, obj1Y, obj2X, obj2Y));
        }

        public Database.Entity GetEntity(int entityID)
        {
            Database.Mob mob;
            Database.Player player;
            Database.Entity entity;
            if (!monsters.TryGetValue(entityID, out mob))
            {
                if (!players.TryGetValue(entityID, out player))
                {
                    //object not existing in mobs nor players
                    return null;
                }
                else
                {
                    entity = (Database.Entity)player;
                }
            }
            else
            {
                entity = (Database.Entity)mob;
            }
            return entity;
        }

        public List<Database.Player> PlayersInSightRange(int x, int y, uint sightRange)
        {
            Map.Nod nod = mapa.GetTileAddress((uint)x, (uint)y); ////////////////////////////////////////////////     CHECK FOR NULL RETURN  !!!!!!!!! 
            if(nod == null)
            {
                List<Database.Player> nullList = new List<Database.Player>();
                return nullList;
            }
            List<int> playersList = mapa.GetAllPlayersAtRange(nod, DEBUG_SIGHT_RANGE);
            List<Database.Player> playersL = new List<Database.Player>(playersList.Count);
            Database.Player p;
            foreach (int i in playersList)
            {
                p = null;
                players.TryGetValue(i, out p);
                if (p != null)
                {
                    playersL.Add(p);
                }
            }
            return playersL;
        }

        public bool AddPlayer(Database.Player player)
        {
            Output.WriteLine("New player at position: " + player.PosX.ToString() + "," + player.PosY.ToString());
            Map.Nod nod = mapa.GetTileAddress((uint)player.PosX, (uint)player.PosY);
            player.MapPosition = nod;
            player.OldMapPosition = new Map.Nod(nod.X, nod.Y);
            if (nod != null)
            {
                Output.WriteLine("Adding player at position: " + nod.X.ToString() + "," + nod.Y.ToString());
            }
            else
            {
                Output.WriteLine(ConsoleColor.Red, "Adding player at position: GetTileAddress return NULL !");
                player.Con.Close();
                return false;
            }
            //Database.Player pl = new Database.Player(con, nod);
            try
            {
                //players.Add(pl.UniqueID, pl);
                players.TryAdd(player.PlayerPID, player);
            }
            catch (ArgumentException e)
            {
                Database.Player tp = null;
                //players.TryGetValue(pl.UniqueID, out tp);
                //players.Remove(pl.UniqueID);
                players.TryRemove(player.PlayerPID, out tp);
                if (tp != null)
                {
                    tp.Con.SendAsync(new Packet.SendPacketHandlers.LoginError(Packet.SendPacketHandlers.LOGIN_ERROR.CURRENTLY_LOGGED));
                    tp.Con.Close();
                }
                AddPlayer(player);
                return true;
            }
            //Output.WriteLine("Adding player at position: " + nod.X.ToString() + "," + nod.Y.ToString());
            if (!mapa.AddEntityAtTile(player.MapPosition, player.PlayerPID, true))
            {
                player.Con.Close();
                return false;
            }
            /*
            else
            {
                //send info to all players in sight range about new player spawn
                List<int> playersList = mapa.GetAllPlayersAtRange(player.MapPosition, DEBUG_SIGHT_RANGE);
                Database.Player p;
                foreach (int i in playersList)
                {
                    p = null;
                    players.TryGetValue(i, out p);
                    if (p != null && p.PlayerPID != player.PlayerPID)
                    {
                        player.AddPlayer(i);
                        p.Con.Send(new Packet.SendPacketHandlers.PlayerSpawn(player.Con));//newly added player data!
                    }
                }
                //inform all mobs in sight range about new player spawn
                List<int> mobsList = mapa.GetAllMobsAtRange(player.MapPosition, DEBUG_SIGHT_RANGE);
                Database.Mob m;
                foreach (int i in mobsList)
                {
                    m = null;
                    monsters.TryGetValue(i, out m);
                    if (m != null)
                    {
                        m.AddPlayer(player.PlayerPID);
                    }
                }
            }
            */
            return true;
        }

        public void RemoveMonster(Database.Mob mb)
        {
            mapa.RemoveEntityAtTile(mb.MapPosition, mb.InternalID, false);
            //monsters.Remove(mb.UniqueID);
            Database.Mob mbOut;
            monsters.TryRemove(mb.InternalID, out mbOut);
        }

        public void RemovePlayer(Database.Player pl)
        {
            mapa.RemoveEntityAtTile(pl.MapPosition, pl.PlayerPID, true);
            //players.Remove(pl.UniqueID);
            Database.Player plOut;
            players.TryRemove(pl.PlayerPID, out plOut);
        }

        public void RemovePlayer(int userID, int playerID)
        {
            if (userID > 0)
            {
                Database.Player tmpPlayer = null;
                //players.TryGetValue(playerID, out tmpPlayer);
                try
                {
                    //players.Remove(playerID);
                    players.TryRemove(playerID, out tmpPlayer);
                }
                catch (Exception e)
                {
                    Output.WriteLine(ConsoleColor.Red, "Error removing player : " + e.ToString());
                    return;
                }
                Database.Player tp;
                if (tmpPlayer != null)
                {
                    bool isOK = mapa.RemoveEntityAtTile(tmpPlayer.MapPosition, tmpPlayer.PlayerPID, true);
                    if (!isOK)
                    {
                        tmpPlayer.Con.Close();
                        Output.WriteLine( ConsoleColor.Red, "Player: " + tmpPlayer.PlayerPID.ToString() + " out of map!");
                        return;
                    }
                    /*
                    List<int> pl = mapa.GetAllPlayersAtRange(tmpPlayer.MapPosition, DEBUG_SIGHT_RANGE);
                    foreach (int i in pl)
                    {
                        tp = null;
                        players.TryGetValue(i, out tp);
                        if (tp != null && tp.PlayerPID != playerID)
                        {
                            tp.Con.Send(new Packet.SendPacketHandlers.PlayerDespawn(playerID));//despawn player!
                        }
                    }
                    */
                }
                Output.WriteLine(ConsoleColor.Magenta, "Player: " + playerID.ToString() + " removed from WORLD");
            }
            else
            {
                Output.WriteLine(ConsoleColor.Red, "Player: " + playerID.ToString() + " NO EXIST in WORLD");
            }
        }

        public List<int> DEBUG_GetAllPlayersAtMap()
        {
            return mapa.DEBUG_GetAllPlayers();
        }

        public void DEBUG_PlayersAtMap()
        {
            mapa.DEBUG_PlayersAtMap();
        }

        public int[] GetCurrentUidList()
        {
            Database.Player[] pList = players.Values.ToArray();
            int[] tmp = new int[pList.Length];
            for (int i = 0; i < pList.Length; i++)
            {
                tmp[i] = pList[i].Con.client.UserID;
            }
            return tmp;
        }

        public int MonstersCount
        {
            get { return monsters.Count; }
        }

        public int PlayerCount
        {
            get { return players.Count; }
        }

        public uint WorldMinX
        {
            get { return mapa.mapMinX; }
        }

        public uint WorldMaxX
        {
            get { return mapa.mapMaxX; }
        }

        public uint WorldMinY
        {
            get { return mapa.mapMinY; }
        }

        public uint WorldMaxY
        {
            get { return mapa.mapMaxY; }
        }

        public int RealWorldX
        {
            get { return mapa.realX; }
        }

        public int RealWorldY
        {
            get { return mapa.realY; }
        }

        public Bitmap GetWorldImage(Bitmap source)
        {
            return mapa.GetWorldImage(source);
        }


        public void BroadcastPacket(Database.Player pl, Packet.SendPacketHandlers.Packet packet)
        {
            BroadcastPacket(pl.PosX, pl.PosY, World.DEBUG_SIGHT_RANGE, packet);
        }

        public void BroadcastPacket(int mapX, int mapY, uint range, Packet.SendPacketHandlers.Packet packet)
        {
            //Output.WriteLine("World::BroadcastPacket From " + mapX.ToString() + "," + mapY.ToString() + "] Range: " + range.ToString());
            List<Database.Player> plList = PlayersInSightRange(mapX, mapY, range);
            //Output.WriteLine("World::BroadcastPacket Broadcast to " + plList.Count.ToString() + " players");
            BroadcastPacket(plList, packet);
        }

        public void BroadcastPacket(List<Database.Player> plList, Packet.SendPacketHandlers.Packet packet)
        {
            foreach (Database.Player p in plList)
            {
                p.Con.SendAsync(new Packet.SendPacketHandlers.CopyPacket(packet));
                //if(packet.PacketType == (byte)Packet.SendPacketHandlers.SEND_HEADER.MOB_DESPAWN) Output.WriteLine("World::BroadcastPacket Send packet " + packet.PacketType.ToString() + " to " + p.Con.client.PlayerID.ToString());
                //Output.WriteLine("World::BroadcastPacket Send packet " + packet.PacketType.ToString() + " to " + p.Con.client.PlayerID.ToString());
            }
        }

        public void MovePlayerOnMap(Database.Player pl, Map.Nod from, Map.Nod to)
        {
            //Output.WriteLine("World::MovePlayerOnMap From [" + from.X.ToString() + "," + from.Y.ToString() + "] To [" + to.X.ToString() + "," + to.Y.ToString() + "]");
            mapa.RemoveEntityAtTile(from, pl.PlayerPID, true);
            if (!mapa.AddEntityAtTile(to, pl.PlayerPID, true))
            {
                pl.Con.Close();
                return;
            }
            /*
            else
            {
                //send info to all players in sight range about new player spawn
                List<int> playersList = mapa.GetAllPlayersAtRange(to, DEBUG_SIGHT_RANGE);
                Database.Player p;
                foreach (int i in playersList)
                {
                    if (!pl.ContainsPlayer(i))//this is new player in range so send spawn and add to known list
                    {
                        p = null;
                        players.TryGetValue(i, out p);
                        if (p != null && p.PlayerPID != pl.PlayerPID)
                        {
                            pl.AddPlayer(i);
                            p.Con.Send(new Packet.SendPacketHandlers.PlayerSpawn(pl.Con));//newly added player data!
                            //if (pl.moving)//player is moving so send this info to new player in range
                            //{
                            //    p.Con.Send(new Packet.SendPacketHandlers.MoveStart(pl.PlayerPID, pl.newX, pl.newY));//newly added player data!
                            //}
                        }
                    }
                }
                List<int> oldPlayersList = pl.KnownPlayers();
                foreach (int i in oldPlayersList)
                {
                    if (!playersList.Contains(i))//this player isn't in range so send despawn
                    {
                        p = null;
                        players.TryGetValue(i, out p);
                        if (p != null && p.PlayerPID != pl.PlayerPID)
                        {
                            pl.RemovePlayer(i);
                            pl.Con.Send(new Packet.SendPacketHandlers.PlayerDespawn(i));//newly added player data!
                        }
                    }
                }
                //check for new mobs in range
                List<int> mobList = mapa.GetAllMobsAtRange(to, DEBUG_SIGHT_RANGE);
                Database.Mob m;
                foreach (int i in mobList)
                {
                    if (!pl.ContainsMob(i))//this is new mob in range so send spawn and add to known list
                    {
                        m = null;
                        monsters.TryGetValue(i, out m);
                        if (m != null)
                        {
                            pl.AddMob(i);
                            m.AddPlayer(pl.PlayerPID);
                            pl.Con.Send(new Packet.SendPacketHandlers.MobSpawn(m));//newly added player data!
                            //if (m.moving)//mob is moving so send this info to new player in range
                            //{
                            //    pl.Con.Send(new Packet.SendPacketHandlers.MobMoveStart(m.InternalID, m.newX, m.newY));//newly added player data!
                            //}
                        }
                    }
                }
                List<int> oldMobsList = pl.KnownMobs();
                foreach (int i in oldMobsList)
                {
                    if (!mobList.Contains(i))//this mob isn't in range so send despawn
                    {
                        m = null;
                        monsters.TryGetValue(i, out m);
                        if (m != null)
                        {
                            pl.RemoveMob(i);
                            pl.Con.Send(new Packet.SendPacketHandlers.MobDespawn(i));//newly added player data!
                        }
                    }
                }
            }
            */
        }

        public void MoveMobOnMap(Database.Mob mb, Map.Nod from, Map.Nod to)
        {
            //Output.WriteLine("MoveMobOnMap ID " + mb.InternalID.ToString() + " at [" + mb.PosX.ToString() + "," + mb.PosY.ToString() + "]");
            bool rem = mapa.RemoveEntityAtTile(from, mb.InternalID, false);
            if (rem)
            {
                //Output.WriteLine("Removed mob ID " + mb.InternalID.ToString() + " at [" + from.X.ToString() + "," + from.Y.ToString() + "]");
            }
            else
            {
                Output.WriteLine("FAILED to remove mob ID " + mb.InternalID.ToString() + " at [" + from.X.ToString() + "," + from.Y.ToString() + "]");
            }
            if (!mapa.AddEntityAtTile(to, mb.InternalID, false))
            {
                //Output.WriteLine("Fail add mob ID " + mb.InternalID.ToString() + " at [" + to.X.ToString() + "," + to.Y.ToString() + "]");
                mb.Despawn(null);
                return;
            }
            /*
            else
            {
                //Output.WriteLine("Added mob ID " + mb.InternalID.ToString() + " at [" + to.X.ToString() + "," + to.Y.ToString() + "]");
                //send info to all players in sight range about new mob spawn
                List<int> playersList = mapa.GetAllPlayersAtRange(to, DEBUG_SIGHT_RANGE);
                Database.Player p;
                foreach (int i in playersList)
                {
                    if (!mb.ContainsPlayer(i))//this is new player in range so send spawn and add to known list
                    {
                        p = null;
                        players.TryGetValue(i, out p);
                        if (p != null )
                        {
                            mb.AddPlayer(i);
                            //p.Con.Send(new Packet.SendPacketHandlers.MobSpawn(mb));//newly added player data!
                            //if (mb.moving)//mob is moving so send this info to new player in range
                            //{
                            //    p.Con.Send(new Packet.SendPacketHandlers.MobMoveStart(mb.InternalID, mb.newX, mb.newY));//newly added player data!
                            //}
                        }
                    }
                }
                List<int> oldPlayersList = mb.KnownPlayers();
                foreach (int i in oldPlayersList)
                {
                    if (!playersList.Contains(i))//this player isn't in range so send despawn
                    {
                        p = null;
                        players.TryGetValue(i, out p);
                        if (p != null)
                        {
                            mb.RemovePlayer(i);
                            //p.Con.Send(new Packet.SendPacketHandlers.MobDespawn(mb.InternalID));//newly added player data!
                        }
                    }
                }
            }
            */
        }

    }
}
