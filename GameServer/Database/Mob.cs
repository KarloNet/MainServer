using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GameServer.AI;
using System.Diagnostics;

namespace GameServer.Database
{
    class Mob : Entity, AI.Ism
    {
        object locker = new object();
        private int AUTOBEHEAD_TIME = 4000;
        private int DESPAWN_TIME = 1000;
        private Timer beheadTimer;
        private Timer despawnTimer;

        CoreStateMachine cSM;

        //for move server side
        GameServerMainLoop.Vector3 moveTarget = new GameServerMainLoop.Vector3();
        float directionX = 0;
        float directionY = 0;
        float mX = 0;
        float mY = 0;
        float distance = 0;
        float speed = 5f;
        int tmX = 0;
        int tmY = 0;
        float startX = 0;
        float startY = 0;
        float tstartX = 0;
        float tstartY = 0;
        public bool moving = false;
        Stopwatch sw = new Stopwatch();
        //
        //public int sightRange = 5;

        //bool isAttacked;
        //int attackerID;
        //int rageVal;

        List<int> knownPlayers = new List<int>();

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

        public Mob(Map.Nod start, Map.Spawn pSpawn, byte type) : base(start, pSpawn, type)
        {
            Map.Nod nod = GameServer.world.GetPositionAtTile(start.X, start.Y);
            PosX = (int)nod.X;
            PosY = (int)nod.Y;
            cSM = new CoreStateMachine();
            ActHealth = 1000;
        }

        private void OnDie(Player pAttacker)
        {
            base.IsKilled = true;

            /* Debug message */
            Output.WriteLine("[MOB: " + InternalID.ToString() + "] Got killed!");
            //broadcastPacket(new PlayAnimation(uniqueID, 8));
            //pAttacker.sendPacket(new PlayAnimation(uniqueID, 8));
            beheadTimer = new Timer(new TimerCallback(OnBehead), pAttacker, AUTOBEHEAD_TIME, Timeout.Infinite);
        }

        public void OnBehead(Object pAttacker)
        {
            if (beheadTimer != null) beheadTimer.Dispose();
            despawnTimer = new Timer(new TimerCallback(Despawn), pAttacker, DESPAWN_TIME, Timeout.Infinite);
            return;
        }

        public void Despawn(Object pAttacker)
        {
            BroadcastPacket bPacket = null;
            if (despawnTimer != null) despawnTimer.Dispose();
            //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MobDespawn(uniqueID));
            bPacket = new BroadcastPacket((uint)PosX, (uint)PosY, (int)World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MobDespawn(uniqueID));
            if (bPacket != null) GameServer.world.BroadcastPacket((int)bPacket.X, (int)bPacket.Y, (uint)bPacket.Range, bPacket.Packet);
            base.Spawn.Respawn();
            //Output.WriteLine("Mob::Despawn MOB ID: " + InternalID.ToString() + " Pos [" + PosX.ToString() + "," + PosY.ToString() + "]");
            GameServer.world.RemoveMonster(this);
            return;
        }

        private void Goto()
        {
            if (Program.DEBUG_MOB_STOP)
            {
                if (currentState == EntityState.STATE.START_MOVE || currentState == EntityState.STATE.MOVE)
                {
                    state = new AI.EntityState.StopMove(PosX, PosY, new Packet.SendPacketHandlers.MobMoveStop(InternalID, PosX, PosY));
                }
                currentState = EntityState.STATE.STOP_MOVE;
                cSM.SetState = AI.CoreStateMachine.State.STAND;
                sw.Stop();
                return;
            }
            float elapsed = sw.ElapsedMilliseconds / 1000f;
            sw.Restart();
            if (newX != 0 && newY != 0)
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
                        //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MobMoveStart(uniqueID, newX, newY));
                        currentState = EntityState.STATE.START_MOVE;
                        state = new AI.EntityState.StartMove(PosX, PosY, new Packet.SendPacketHandlers.MobMoveStart(InternalID, newX, newY));
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
                        if (Math.Sqrt(Math.Pow(tstartX - startX, 2) + Math.Pow(tstartY - startY, 2)) >= distance)
                        {
                            PosX = newX;
                            PosY = newY;
                            moving = false;
                            //pl.Con.Send(new Packet.SendPacketHandlers.MoveStop(pl.PlayerPID, pl.tmX, pl.tmY));//send stop packet to user
                            //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MobMoveStop(uniqueID, tmX, tmY));
                            currentState = EntityState.STATE.STOP_MOVE;
                            state = new AI.EntityState.StopMove(PosX, PosY, new Packet.SendPacketHandlers.MobMoveStop(InternalID, PosX, PosY));
                            newX = newY = 0;
                            Map.Nod curNod = GameServer.world.GetTileAddress((uint)PosX, (uint)PosY);
                            if(curNod == null)
                            {
                                curNod = GameServer.world.GetPositionAtTile(MapPosition.X, MapPosition.Y);
                                if(curNod == null)
                                {
                                    Output.WriteLine( ConsoleColor.Red, "Mob::Goto Nod null!");
                                    Despawn(null);
                                    return;
                                }
                                PosX = (int)curNod.X;
                                PosY = (int)curNod.Y;
                                Despawn(null);
                                return;
                            }
                            if (curNod.X != MapPosition.X || curNod.Y != MapPosition.Y)
                            {
                                //player moved to new map tile
                                if (PosX > GameServer.world.WorldMaxX || PosX < GameServer.world.WorldMinX || PosY > GameServer.world.WorldMaxY || PosY < GameServer.world.WorldMinY)
                                {
                                    Despawn(null);
                                    return;
                                }
                                MapPosition.X = curNod.X;
                                MapPosition.Y = curNod.Y;
                                GameServer.world.MoveMobOnMap(this, OldMapPosition, MapPosition);
                                OldMapPosition.X = curNod.X;
                                OldMapPosition.Y = curNod.Y;
                            }
                            cSM.SetState = AI.CoreStateMachine.State.STAND;
                            sw.Stop();
                        }
                        else
                        {
                            if (PosX == newX && PosY == newY)
                            {
                                moving = false;
                                //pl.Con.Send(new Packet.SendPacketHandlers.MoveStop(pl.PlayerPID, pl.tmX, pl.tmY));
                                //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MobMoveStop(uniqueID, tmX, tmY));
                                currentState = EntityState.STATE.STOP_MOVE;
                                state = new AI.EntityState.StopMove(PosX, PosY, new Packet.SendPacketHandlers.MobMoveStop(InternalID, PosX, PosY));
                                Map.Nod curNod = GameServer.world.GetTileAddress((uint)PosX, (uint)PosY);
                                newX = newY = 0;
                                if (curNod == null)
                                {
                                    curNod = GameServer.world.GetPositionAtTile(MapPosition.X, MapPosition.Y);
                                    if (curNod == null)
                                    {
                                        Output.WriteLine(ConsoleColor.Red, "Mob::Goto Nod null!");
                                        Despawn(null);
                                        return;
                                    }
                                    Despawn(null);
                                    return;
                                }
                                if (curNod.X != MapPosition.X || curNod.Y != MapPosition.Y)
                                {
                                    //player moved to new map tile
                                    if (PosX > GameServer.world.WorldMaxX || PosX < GameServer.world.WorldMinX || PosY > GameServer.world.WorldMaxY || PosY < GameServer.world.WorldMinY)
                                    {
                                        Despawn(null);
                                        return;
                                    }
                                    MapPosition.X = curNod.X;
                                    MapPosition.Y = curNod.Y;
                                    GameServer.world.MoveMobOnMap(this, OldMapPosition, MapPosition);
                                    OldMapPosition.X = curNod.X;
                                    OldMapPosition.Y = curNod.Y;
                                }
                                cSM.SetState = AI.CoreStateMachine.State.STAND;
                                sw.Stop();
                                return;
                            }
                            //Output.WriteLine("[" + mX.ToString() +"," + mY.ToString() + "] to [" + tstartX.ToString() + "," + tstartY.ToString() + "]");
                            //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MobMove(uniqueID, tmX, tmY));
                            currentState = EntityState.STATE.MOVE;
                            state = new AI.EntityState.Move(PosX, PosY, null);//new AI.EntityState.Move(PosX, PosY, new Packet.SendPacketHandlers.MobMove(InternalID, tmX, tmY));
                            int bX = (int)tstartX - PosX;
                            int bY = (int)tstartY - PosY;
                            if (bX != 0 || bY != 0)
                            {
                                // update the own position
                                PosX += bX;
                                PosY += bY;
                                PosZ += 0;
                                Map.Nod curNod = GameServer.world.GetTileAddress((uint)PosX, (uint)PosY);
                                if (curNod == null)
                                {
                                    curNod = GameServer.world.GetPositionAtTile(MapPosition.X, MapPosition.Y);
                                    if (curNod == null)
                                    {
                                        Output.WriteLine(ConsoleColor.Red, "Mob::Goto Nod null!");
                                        Despawn(null);
                                        return;
                                    }
                                    Despawn(null);
                                    return;
                                }
                                if (curNod.X != MapPosition.X || curNod.Y != MapPosition.Y)
                                {
                                    //player moved to new map tile
                                    if (PosX > GameServer.world.WorldMaxX || PosX < GameServer.world.WorldMinX || PosY > GameServer.world.WorldMaxY || PosY < GameServer.world.WorldMinY)
                                    {
                                        Despawn(null);
                                        return;
                                    }
                                    MapPosition.X = curNod.X;
                                    MapPosition.Y = curNod.Y;
                                    GameServer.world.MoveMobOnMap(this, OldMapPosition, MapPosition);
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
                    //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MobMoveStop(uniqueID, tmX, tmY));
                    currentState = EntityState.STATE.STOP_MOVE;
                    state = new AI.EntityState.StopMove(PosX, PosY, new Packet.SendPacketHandlers.MobMoveStop(InternalID, PosX, PosY));
                    Map.Nod curNod = GameServer.world.GetTileAddress((uint)PosX, (uint)PosY);
                    newX = newY = 0;
                    if (curNod == null)
                    {
                        curNod = GameServer.world.GetPositionAtTile(MapPosition.X, MapPosition.Y);
                        if (curNod == null)
                        {
                            Output.WriteLine(ConsoleColor.Red, "Mob::Goto Nod null!");
                            Despawn(null);
                            return;
                        }
                        Despawn(null);
                        return;
                    }
                    if (curNod.X != MapPosition.X || curNod.Y != MapPosition.Y)
                    {
                        //player moved to new map tile
                        if (PosX > GameServer.world.WorldMaxX || PosX < GameServer.world.WorldMinX || PosY > GameServer.world.WorldMaxY || PosY < GameServer.world.WorldMinY)
                        {
                            Despawn(null);
                            return;
                        }
                        MapPosition.X = curNod.X;
                        MapPosition.Y = curNod.Y;
                        GameServer.world.MoveMobOnMap(this, OldMapPosition, MapPosition);
                        OldMapPosition.X = curNod.X;
                        OldMapPosition.Y = curNod.Y;
                    }
                    cSM.SetState = AI.CoreStateMachine.State.STAND;
                    sw.Stop();
                    return;
                }
            }
        }

        void Ism.Wonder()
        {
            //////////////////////////////////////////////////////////////////////////////
            //cSM.SetState = AI.CoreStateMachine.State.STAND;
            //return;
            /////////////////////////////////////////////////////////////////////////////
            if (IsAttacked)
            {
                cSM.SetState = AI.CoreStateMachine.State.ATTACK;
                return;
            }
            if (!moving)
            {
                int change = Program.random.Next(10);
                if (change > 1)
                {
                    cSM.SetState = AI.CoreStateMachine.State.STAND;
                    return;
                }
                else
                {
                    int flip = Program.random.Next(2);
                    int radd = Program.random.Next(15);
                    if (flip == 0)
                    {
                        //newX = PosX + radd;
                        newX = PosX + radd;
                        //Output.WriteLine("Flip 0 X = " + radd.ToString());
                    }
                    else
                    {
                        newX = PosX - radd;
                        //Output.WriteLine("Flip 1 X = -" + radd.ToString());
                    }
                    flip = Program.random.Next(2);
                    radd = Program.random.Next(15);
                    if (flip == 0)
                    {
                        newY = PosY + radd;
                        //newY = PosY + radd;
                        //Output.WriteLine("Flip 0 Y = " + radd.ToString());
                    }
                    else
                    {
                        newY = PosY - radd;
                        //Output.WriteLine("Flip 1 Y = -" + radd.ToString());
                    }
                    moving = true;
                    //Output.WriteLine("Mob in WONDER start state move to [" + newX.ToString() + "," + newY.ToString() + "]");
                }
            }

        }

        void Ism.Stand()
        {
            if (IsAttacked)
            {
                cSM.SetState = AI.CoreStateMachine.State.ATTACK;
                return;
            }
            int change = Program.random.Next(10);
            if(change > 5)
            {
                cSM.SetState = AI.CoreStateMachine.State.WONDER;
            }
        }

        void Ism.Attack()
        {
            Output.WriteLine("Mob in ATTACK state");
            if (moving)//stop mob on current position...
            {
                /*
                moving = false;
                tmX = (int)(PosX * 10000);
                tmY = (int)(PosY * 10000);
                //GameServer.world.BroadcastPacket(PosX, PosY, World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.MoveStop(uniqueID, tmX, tmY));
                currentState = EntityState.STATE.STOP_MOVE;
                Map.Nod curNod = GameServer.world.GetTileAddress((uint)PosX, (uint)PosY);
                if (curNod.X != MapPosition.X || curNod.Y != MapPosition.Y)
                {
                    //player moved to new map tile
                    MapPosition.X = curNod.X;
                    MapPosition.Y = curNod.Y;
                    GameServer.world.MoveMobOnMap(this, OldMapPosition, MapPosition);
                    OldMapPosition.X = curNod.X;
                    OldMapPosition.Y = curNod.Y;
                }
                */
            }
            //get the attacker
            Database.Entity attEntity = null;
            while (true)
            {
                int attackerId = AttackerID;
                if (attackerId == -1)
                {
                    //no attackers in list, back to stand state and return;
                    IsAttacked = false;
                    cSM.SetState = AI.CoreStateMachine.State.STAND;
                    Output.WriteLine("No attackers found");
                    return;
                }
                attEntity = GameServer.world.GetEntity(attackerId);
                if (attEntity == null)
                {
                    RemoveAttacker(attackerId);
                    Output.WriteLine("Remove attacke that no longer exists");
                }
                else
                {
                    //continue with attacker we got from list
                    //Output.WriteLine("Got attacker from list  - continue");
                    break;
                }
            }
            //continue
            if(attEntity == null)
            {
                //shouldn't happens..
                IsAttacked = false;
                cSM.SetState = AI.CoreStateMachine.State.STAND;
                Output.WriteLine("Enity is null! - shouldn't happens...");
                return;
            }
            //check range from attacker, if far go to him if we are close hit him
            int distance = GameServer.world.GetDistance(PosX, PosY, attEntity.PosX, attEntity.PosY);
            if(distance > AttackRange)
            {
                //goto attacker
                newX = attEntity.PosX;
                newY = attEntity.PosY;
                moving = true;
                //Output.WriteLine("Distance to attacker is biger then aattackRange -> go to him");
                //Output.WriteLine("GoTo: " + newX.ToString() + "," + newY.ToString() + " Distance: " + distance.ToString());
            }
            else
            {
                //hit attacker
                Output.WriteLine("MOB HIT ATTACKER..");
                IsAttacked = false;
                cSM.SetState = AI.CoreStateMachine.State.WONDER;
            }
        }

        void Ism.Run()
        {
            Output.WriteLine("Mob in RUN state");
        }

        void Ism.Update(Ism entityIsm)
        {
            if (!IsKilled)
            {
                cSM.Update(entityIsm);
                if (moving) Goto();
            }
            else
            {
                if (despawnTimer == null)
                {
                    OnBehead(null);
                }
                //Despawn(null);
            }
        }

        EntityState.STATE Ism.GetCurrentState()
        {
            throw new NotImplementedException();
        }

        void Ism.UpdateFrame(EntityState.State entityState)
        {
            throw new NotImplementedException();
        }
    }
}
