using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Packet;

namespace GameServer.AI
{
    public class EntityState
    {
        public enum STATE
        {
            STAND = 0,
            START_MOVE = 1,
            MOVE = 2,
            STOP_MOVE = 3,
            ATTACK = 4
        }

        public abstract class State
        {
            public STATE currentState;
            public STATE oldState;
            internal Packet.SendPacketHandlers.Packet packet;

            internal State(Packet.SendPacketHandlers.Packet packet)
            {
                this.packet = packet;
            }
            internal abstract void BroadcastState(int posX, int posY);
            internal void SendState(Connection con)
            {
                if(con != null && packet != null) con.SendAsync(packet);
            }
        }

        public class Idle : State
        {
            internal Idle(SendPacketHandlers.Packet packet) : base(packet)
            {
            }

            internal override void BroadcastState(int posX, int posY)
            {
                
            }
        }

        public class StartMove : State
        {
            int newX;
            int newY;

            internal StartMove(int x, int y, Packet.SendPacketHandlers.Packet packet) : base(packet)
            {
                newX = x;
                newY = y;
            }

            internal override void BroadcastState(int posX, int posY)
            {
                GameServer.world.BroadcastPacket(posX, posY, World.DEBUG_SIGHT_RANGE, packet);
            }
        }

        public class Move : State
        {
            public int newX;
            public int newY;

            internal Move(int x, int y, Packet.SendPacketHandlers.Packet packet) : base (packet)
            {
                newX = x;
                newY = y;
            }

            internal override void BroadcastState(int posX, int posY)
            {
                GameServer.world.BroadcastPacket(posX, posY, World.DEBUG_SIGHT_RANGE, packet);
            }
        }

        public class StopMove : State
        {
            public int newX;
            public int newY;

            internal StopMove(int x, int y, Packet.SendPacketHandlers.Packet packet) : base (packet)
            {
                newX = x;
                newY = y;
            }

            internal override void BroadcastState(int posX, int posY)
            {
                GameServer.world.BroadcastPacket(posX, posY, World.DEBUG_SIGHT_RANGE, packet);
            }
        }

    }
}
