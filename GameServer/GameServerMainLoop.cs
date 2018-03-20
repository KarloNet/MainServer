using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace GameServer
{
    class GameServerMainLoop
    {
        public class Vector3
        {
            float x;
            float y;
            float z;

            public float X { get { return this.x; } set { this.x = value; } }
            public float Y { get { return this.y; } set { this.y = value; } }
            public float Z { get { return this.z; } set { this.z = value; } }

            public Vector3(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public Vector3()
            {
                this.x = 0;
                this.y = 0;
                this.z = 0;
            }
            public Vector3(Vector3 v)
            {
                this.x = v.x;
                this.y = v.y;
                this.z = v.z;
            }

            public void Normalize()
            {
                float length = (float)Math.Sqrt((double)((this.x * this.x) + (this.y * this.y) + (this.z * this.z)));
                this.x = this.x / length;
                this.y = this.y / length;
                this.z = this.z / length;
            }
        };

        private static class MoveMath
        {
            private static float differance;

            public static float Lerp(float from, float to, float percent)
            {
                differance = to - from;
                return from + (differance * percent);
            }

            public static bool FastApproximately(float a, float b, float threshold)
            {
                return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
            }

            public static bool FastApproximately(Vector3 a, Vector3 b, float threshold)
            {
                bool xCheck = ((a.X - b.X) < 0 ? ((a.X - b.X) * -1) : (a.X - b.X)) <= threshold;
                bool yCheck = ((a.Y - b.Y) < 0 ? ((a.Y - b.Y) * -1) : (a.Y - b.Y)) <= threshold;
                bool zCheck = ((a.Z - b.Z) < 0 ? ((a.Z - b.Z) * -1) : (a.Z - b.Z)) <= threshold;
                if (xCheck && yCheck && zCheck)
                    return true;
                else
                    return false;
            }
        }

        //public static ManualResetEvent waitForPacket = new ManualResetEvent(true);
        private static Timer tickTimer;

        private const short WAKEUP_INTERVAL = 100;
        private const float GAME_TICK = (float)WAKEUP_INTERVAL / 1000;
        public bool stopMainLoop = false;
        private static bool prepareFrame = true;
        public static bool noSleep = false;

        public void Run()
        {
            Output.WriteLine(ConsoleColor.Green ,"Main Game Server Thread started!");
            int plCount = 0;
            int mobCount = 0;
            tickTimer = new Timer(OnCallBack, null, 100, 100);
            while (!stopMainLoop)
            {
                //waitForPacket.WaitOne();

                if (prepareFrame)
                {
                    //AI
                    foreach (Database.Mob mb in GameServer.world.monsters.Values)
                    {
                        mb.state = null;//new AI.EntityState.Idle(null);
                        AI.Ism mySM = mb;
                        mySM.Update(mb);
                        mobCount++;
                    }
                    //player move
                    foreach (Database.Player pl in GameServer.world.players.Values)
                    {
                        pl.state = null;// new AI.EntityState.Idle(null);
                        AI.Ism mySM = pl;
                        mySM.Update(pl);
                        plCount++;
                    }
                    //broadcast changes to clients
                    foreach (Database.Player pl in GameServer.world.players.Values)
                    {
                        AI.Ism mySM = pl;
                        mySM.UpdateFrame(pl.state);
                    }

                    if (Program.DEBUG_Main_Loop)
                    {
                        Output.WriteLine("Main Loop check Players: " + plCount.ToString());
                        Output.WriteLine("Main Loop check Mobs: " + mobCount.ToString());
                    }
                    prepareFrame = false;
                }
                else
                {
                    //check only if thers any packet left to send
                    foreach (Database.Player pl in GameServer.world.players.Values)
                    {
                        if (pl.Con.IsPacketWaitingToSend)
                        {
                            pl.Con.SendAsync(null);
                        }
                        if (pl.Con.IsPacketWaitingToSend) noSleep = true;
                    }

                    if(GameServer.world.broadcastQueue.Count > 0)
                    {
                        BroadcastPacket bPacket = null;
                        GameServer.world.broadcastQueue.TryDequeue(out bPacket);
                        if(bPacket != null)
                        {
                            GameServer.world.BroadcastPacket((int)bPacket.X , (int)bPacket.Y, (uint)bPacket.Range, bPacket.Packet);
                        }
                        if (GameServer.world.broadcastQueue.Count > 0) noSleep = true;
                    }

                    if (GameServer.world.actions.Count > 0)
                    {
                        Skill.Action action = null;
                        GameServer.world.actions.TryDequeue(out action);
                        if (action != null)
                        {
                            //do action
                            action.DoAction();
                        }
                        if (GameServer.world.actions.Count > 0) noSleep = true;
                    }

                }
                plCount = mobCount = 0;
                //Thread.Sleep(WAKEUP_INTERVAL / 10);
                //waitForPacket.Reset();
                if (!noSleep)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    noSleep = false;
                }
            }
            Output.WriteLine(ConsoleColor.Yellow, "Main Game Server Thread stopped!");
        }

        private static void OnCallBack(object state)
        {
            prepareFrame = true;
            //waitForPacket.Set();
        }

    }
}