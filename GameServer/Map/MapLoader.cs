using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GameServer.Map
{
    class MapLoader
    {
        Bitmap mapData;
        MapData md;
        List<Map.Spawn> spawns;

        public MapLoader()
        {
            spawns = new List<Map.Spawn>();
        }

        public MapData LoadMap(string map, World world)
        {
            this.mapData = new Bitmap(map + ".png");
            md = new MapData(this.mapData, world);
            IniFile configMapFile = new IniFile( map + ".ini");
            Map.SpawnArea sa;
            Map.Spawn sp;
            int secX = 0;
            int secY = 0;
            int secW = 0;
            int secH = 0;
            int mCount = 0;
            int mType = 0;
            int mRespawn = 0;
            foreach(string sec in configMapFile.Sections)
            {
                secX = configMapFile.GetInteger(sec, "spawn_x", 0);
                secY = configMapFile.GetInteger(sec, "spawn_y", 0);
                secW = configMapFile.GetInteger(sec, "spawn_width", 0);
                secH = configMapFile.GetInteger(sec, "spawn_height", 0);
                mCount = configMapFile.GetInteger(sec, "mob_count", 0);
                mType = configMapFile.GetInteger(sec, "mob_type", 0);
                mRespawn = configMapFile.GetInteger(sec, "mob_resp_time", 0);
                if(secX < 0 || secX > md.realX || secY < 0 || secY > md.realY || secW == 0 || secH == 0 || mCount == 0)
                {
                    Output.WriteLine("Error initialize spawn: " + sec);
                    Output.WriteLine("     X: " + secX.ToString() + " Y: " + secY.ToString() + " W: " + secW.ToString() + " H: " + secH.ToString() + " Type: " + mType.ToString() + " Count: " + mCount.ToString());
                }
                else
                {
                    sa = new Map.SpawnArea(secX, secY, secW, secH, md.mapa);
                    sp = new Map.Spawn(mCount, mType, mRespawn, sa, world);
                    spawns.Add(sp);
                }
            }
            return md;
        }

        public bool Init()
        {
            md.Init();
            InitSpawns();
            return true;
        }

        private bool InitSpawns()
        {
            foreach (Map.Spawn s in spawns)
            {
                s.Init();
            }
            return true;
        }
    }
}
