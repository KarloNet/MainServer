using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GameServer.Map;

namespace GameServer.Entity
{
    class Monster : Entity
    {
        private int AUTOBEHEAD_TIME = 4000;
        private int DESPAWN_TIME = 1000;
        private Timer beheadTimer;
        private Timer despawnTimer;

        public int currentHP = 890;
        public int maxHP = 890;

        
        public Monster(Nod start, Spawn pSpawn, byte type) : base(start, pSpawn, type)
        {
            Nod nod = GameServer.world.GetPositionAtTile(start.X, start.Y);
            posX = (int)nod.X;
            posY = (int)nod.Y;
        }

        private void OnDie(Player pAttacker)
        {
            base.IsKilled = true;

            /* Debug message */
            Output.WriteLine("[MOB: " + UniqueID.ToString() + "] Got killed!");

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
            if (despawnTimer != null) despawnTimer.Dispose();
            //broadcastPacket(new Despawn(uniqueID));
            base.Spawn.Respawn();
            return;
        }

        public void broadcastPacket(Packet.SendPacketHandlers p)
        {
            //foreach (Player otherPlayer in knownObjects)
            //{
            //    otherPlayer.sendPacket(p);
            //}
        }

    }
}
