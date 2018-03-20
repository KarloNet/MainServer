using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace GameServer.Map
{
    public class Nod
    {
        public uint X;
        public uint Y;

        public Nod()
        {
            X = Y = 0;
        }

        public Nod(uint x, uint y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    class SpawnArea
    {
        static Random rnd = new Random(Environment.TickCount);
        public MapData.Data[,] map;
        public int x;
        public int y;
        public int width;
        public int height;

        public SpawnArea()
        {
            x = y = width = height = 0;
            map = null;
        }

        public SpawnArea(int x, int y, int width, int height, MapData.Data[,] mapData)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.map = mapData;
        }

        /* Look randomly for a free node in the map         *
        * until one is found where the MOB can spawn       */
        public Nod SearchFreeNode()
        {
            Nod start = new Nod();
            int howLong = 0;
            while (true)
            {
                start.X = (uint)(rnd.Next(this.x, this.x + width));
                start.Y = (uint)(rnd.Next(this.y, this.y + height));
                if (map[start.X, start.Y] != null)
                {
                    if (map[start.X, start.Y].CountEntitys == 0)
                    {
                        return start;
                    }
                }
                howLong++;
                if(howLong > 1000)
                {
                    Output.WriteLine("Spawn:SpawnArea::SearchFreeNode Took too long!");
                    while (true)
                    {
                        start.X = (uint)(rnd.Next(this.x, this.x + width));
                        start.Y = (uint)(rnd.Next(this.y, this.y + height));
                        if (map[start.X, start.Y] != null)
                        {
                            //for (int i = map[start.X, start.Y].CountEntitys - 1; i >= 0; i--)
                            //{
                            //    Output.WriteLine("   Enity ID: " + map[start.X, start.Y].EntitysIDs[i].ToString());
                            //}
                            return start;
                        }
                    }
                    return null;
                }
            }
        }
    }

    class Spawn
    {
        static int nextSpawnID = 101;
        int spawnID;
        int entityCount;
        int entityType;
        int entityRespawnTime;
        SpawnArea spawn;
        World world;

        public Spawn(int count, int type, int respTime, SpawnArea spawnArea, World world)
        {
            spawnID = Interlocked.Increment(ref nextSpawnID);
            entityCount = count;
            entityType = type;
            entityRespawnTime = respTime;
            spawn = spawnArea;
            this.world = world;
        }

        public void Init()
        {
            InitSpawn();
            Output.WriteLine("Spawn: " + spawnID.ToString() + " initialized");
        }

        public int SpawnID
        {
            get { return spawnID; }
        }

        private void InitSpawn()
        {
            for(int i = 0; i < entityCount; i++)
            {
                Nod start = spawn.SearchFreeNode();
                if (start != null)
                {
                    Database.Mob mob = new Database.Mob(start, this, 1);
                    world.AddMonster(mob);
                }
            }
        }

        public void Respawn()
        {
            while (true)
            {
                Nod start = spawn.SearchFreeNode();
                if (start != null)
                {
                    Database.Mob mob = new Database.Mob(start, this, 1);
                    //Output.WriteLine("Spawn::Respawn Mob ID: " + mob.InternalID.ToString());
                    world.AddMonster(mob);
                    break;
                }
            }
        }

    }

}
