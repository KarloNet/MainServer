using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GameServer.Map;

namespace GameServer.Entity
{
    class Entity
    {
        protected static int nextID = 101;

        protected int uniqueID;
        private byte entityType = 0;

        private Map.Nod myNode;
        private Map.Spawn mySpawn;

        private bool isKilled;
        private Nod start;
        private Spawn spawn;
        public int posX;
        public int posY;

        public Map.Spawn Spawn { get { return this.mySpawn; } }
        public bool IsKilled { get { return this.isKilled; } set { this.isKilled = value; } }
        public Map.Nod MapPosition { get { return this.myNode; } set { this.myNode = value; } }
        public byte Type { get { return this.entityType; } }
        public int UniqueID { get { return this.uniqueID; } }

        public Entity(Map.Nod start, Map.Spawn pSpawn,  byte type, int id)
        {
            this.mySpawn = pSpawn;
            this.myNode = start;
            this.entityType = type;
            this.uniqueID = id;
        }

        public Entity(Map.Nod start, Map.Spawn pSpawn, byte type)
        {
            this.mySpawn = pSpawn;
            this.myNode = start;
            this.entityType = type;
            this.uniqueID = Interlocked.Increment(ref nextID);
        }

        public Entity(Nod start, Spawn spawn)
        {
            this.start = start;
            this.spawn = spawn;
            this.uniqueID = Interlocked.Increment(ref nextID);
        }
    }
}
