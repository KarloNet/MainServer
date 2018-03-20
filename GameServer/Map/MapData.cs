using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GameServer.Map
{
    class MapData
    {
        public class Data
        {
            object cellLocker;
            List<int> playersID;
            List<int> entitysID;
            uint x;
            uint y;
            uint z;

            public Data(uint x, uint y, uint z)
            {
                entitysID = new List<int>();
                playersID = new List<int>();
                this.x = x;
                this.y = y;
                this.z = z;
                cellLocker = new object();
            }

            public uint X { get { return x; } }
            public uint Y { get { return y; } }
            public uint Z { get { return z; } }

            public int[] EntitysIDs
            {
                get
                { int[] copyList;
                    lock (entitysID)
                    {
                        copyList = entitysID.ToArray();
                    }
                    return copyList;
                }
            }
            public int[] PlayersIDs { get { int[] copyList; lock (playersID) { copyList = playersID.ToArray(); } return playersID.ToArray(); } }
            public int CountEntitys { get { return entitysID.Count; } }
            public int CountPlayers { get { return playersID.Count; } }
            public void AddEntity(int entityID)
            {
                lock (entitysID)
                {
                    entitysID.Add(entityID);
                }
            }
            public void RemoveEntity(int entityID)
            {
                lock (entitysID)
                {
                    entitysID.Remove(entityID);
                }
            }
            public void AddPlayer(int playerID)
            {
                lock (playersID)
                {
                    playersID.Add(playerID);
                }
            }
            public void RemovePlayer(int playerID)
            {
                lock (playersID)
                {
                    playersID.Remove(playerID);
                }
            }

        }

        public const int GRIDSIZE = 5;//it shouldn't be changed
        public const int TILESIZE_X = 200;//it shouldn't be changed
        public const int TILESIZE_Y = 200;//it shouldn't be changed
        public const int X_MULTIPLIKATOR = 1;//its world map position ( next map region would be 2 ,3 ect)
        public const int Y_MULTIPLIKATOR = 1;//its world map position
        int mapSizeX;
        int mapSizeY;
        public readonly int realX;
        public readonly int realY;
        public readonly uint mapMinX = 0;
        public readonly uint mapMaxX = 0;
        public readonly uint mapMinY = 0;
        public readonly uint mapMaxY = 0;
        Data[,] mapData;
        public Data[,] mapa { get { return mapData; } }

        public MapData(Image map, World world)
        {
            Bitmap bmp = new Bitmap(map);
            mapSizeX = map.Width;
            mapSizeY = map.Height;
            realX = mapSizeX;
            realY = mapSizeY;
            mapData = new Data[mapSizeX, mapSizeY];
            mapMinX = X_MULTIPLIKATOR * TILESIZE_X * GRIDSIZE;
            mapMaxX = X_MULTIPLIKATOR * TILESIZE_X * GRIDSIZE + ((uint)mapSizeX - 1) * GRIDSIZE;
            mapMinY = Y_MULTIPLIKATOR * TILESIZE_Y * GRIDSIZE;
            mapMaxY = Y_MULTIPLIKATOR * TILESIZE_Y * GRIDSIZE + ((uint)mapSizeY - 1) * GRIDSIZE;
            Output.WriteLine(ConsoleColor.Yellow, "MAP SIZE MIN = [" + mapMinX.ToString() + "," + mapMinY.ToString() + "]");
            Output.WriteLine(ConsoleColor.Yellow, "MAP SIZE MAX = [" + mapMaxX.ToString() + "," + mapMaxY.ToString() + "]");
            for (uint i = 0; i < map.Width; i++)
            {
                for (uint j = 0; j < map.Height; j++)
                {
                    Color c = bmp.GetPixel((int)i,(int)j);
                    //Output.WriteLine("MAP AT [" + i.ToString() + "," + j.ToString() + "] R = " + c.R.ToString() + " G = " + c.G.ToString());
                    if (c.R >= 200)//pixel is full red ( or white if all r,g,b are 255 or add some space for programs that mess with colrs ...
                    {
                        uint tX = X_MULTIPLIKATOR * TILESIZE_X * GRIDSIZE + i * GRIDSIZE;
                        uint tY = Y_MULTIPLIKATOR * TILESIZE_Y * GRIDSIZE + j * GRIDSIZE;
                        uint tZ = 19888;
                        mapData[i, j] = new Data(tX, tY, tZ);
                        //Output.WriteLine("MAP AT [" + i.ToString() + "," + j.ToString() + "] = [" + tX.ToString() + "," + tY.ToString() + "]");
                    }
                }
            }

            if(mapData[0,0] != null) Output.WriteLine(ConsoleColor.Yellow, "Position at [0,0] = [" + mapData[0,0].X.ToString() + "," + mapData[0,0].Y.ToString() + "]" );
            if(mapData[199,199] != null) Output.WriteLine(ConsoleColor.Yellow, "Position at [199,199] = [" + mapData[199, 199].X.ToString() + "," + mapData[199, 199].Y.ToString() + "]");
            GetTileAddress(8192, 8192);
            GetTileAddress(14560, 14560);
        }

        public bool Init()
        {
            return true;
        }

        public Data GetTileAtPosition(uint x, uint y)
        {
            if (x < mapMinX || x > mapMaxX || y < mapMinY || y > mapMaxY) return null;
            uint tx = (x - (X_MULTIPLIKATOR * TILESIZE_X * GRIDSIZE)) / GRIDSIZE;
            uint ty = (y - (Y_MULTIPLIKATOR * TILESIZE_Y * GRIDSIZE)) / GRIDSIZE;
            if (tx >= 0 && tx < mapSizeX && ty >= 0 && ty < mapSizeY && mapData != null)
            {
                return mapData[tx, ty];
            }
            else
            {
                return null;
            }
        }

        public Nod GetPositionAtTile(uint xTile, uint yTile)
        {
            Nod nod = new Nod();
            nod.X = X_MULTIPLIKATOR * TILESIZE_X * GRIDSIZE + xTile * GRIDSIZE;
            nod.Y = Y_MULTIPLIKATOR * TILESIZE_Y * GRIDSIZE + yTile * GRIDSIZE;
            return nod;
        }

        public Nod GetTileAddress(uint xPos, uint yPos)
        {
            if (xPos < mapMinX || xPos > mapMaxX || yPos < mapMinY || yPos > mapMaxY)
            {
                return null;
            }
            Nod nod = new Nod();
            nod.X = (xPos - (X_MULTIPLIKATOR * TILESIZE_X * GRIDSIZE)) / GRIDSIZE;
            nod.Y = (yPos - (Y_MULTIPLIKATOR * TILESIZE_Y * GRIDSIZE)) / GRIDSIZE;
            //Output.WriteLine(ConsoleColor.Cyan, "Position at [" + x.ToString() + "," + y.ToString() + "] = [" + nod.X.ToString() + "," + nod.Y.ToString() + "]");
            return nod;
        }

        public List<int> GetAllPlayersAtRange(uint x, uint y, uint sightRange)
        {
            Nod n = new Nod(x, y);
            return GetAllPlayersAtRange(n, sightRange);
        }
        public List<int> GetAllPlayersAtRange(Nod nod, uint sightRange)
        {
            //Output.WriteLine(ConsoleColor.Yellow, "Check from [" + nod.X.ToString() + "," + nod.Y.ToString() + "] Range: " + sightRange.ToString());
            List<int> tmpList = new List<int>();
            uint sx;
            uint sy;
            uint ex;
            uint ey;
            if(sightRange > nod.X)
            {
                sx = 0;
            }
            else
            {
                sx = nod.X - sightRange;
                if (sx < 0) sx = 0;
            }
            if (sightRange > nod.Y)
            {
                sy = 0;
            }
            else
            {
                sy = nod.Y - sightRange;
                if (sy < 0) sy = 0;
            }
            ex = nod.X + sightRange;
            if (ex > realX) ex = (uint)realX;
            ey = nod.Y + sightRange;
            if (ey > realY) ey = (uint)realY;
            //Output.WriteLine(ConsoleColor.Yellow, "Check after bounds FROM " + sx.ToString() + "," + sy.ToString() + " TO: " + ex.ToString() + "," + ey.ToString());
            for (uint i = sx; i < ex; i++)
            {
                for (uint j = sy; j < ey; j++)
                {
                    if (i >= 0 && i < mapSizeX && j >= 0 && j < mapSizeY)
                    {
                        if (mapData[i, j] != null)
                        {
                            tmpList.AddRange(mapData[i, j].PlayersIDs);
                        }
                    }else
                    {
                        Output.WriteLine(ConsoleColor.Red, "MapData::GetAllPlayersAtRange INDEX OUT OF BOUND " + i.ToString() + "," + j.ToString());
                    }
                }
            }
            HashSet<int> tmHash = new HashSet<int>(tmpList);
            tmpList = new List<int>(tmHash);
            return tmpList;
            /*
            //FOR CIRCLE USE THIS BUT IT WILL SLOW DOWN PROCESS
            //m1 and m2 are x,y of center radius
            List<int> indices = new List<int>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    double dx = x - m1;
                    double dy = y - m2;
                    double distanceSquared = dx * dx + dy * dy;

                    if (distanceSquared <= radiusSquared)
                    {
                        indices.Add(x + y * width);
                    }
                }
            }
            */
        }
        public List<int> GetAllMobsAtRange(uint x, uint y, uint sightRange)
        {
            Nod n = new Nod(x,y);
            return GetAllMobsAtRange(n, sightRange);
        }
        public List<int> GetAllMobsAtRange(Nod nod, uint sightRange)
        {
            List<int> tmpList = new List<int>();
            uint sx;
            uint sy;
            uint ex;
            uint ey;
            if (sightRange > nod.X)
            {
                sx = 0;
            }
            else
            {
                sx = nod.X - sightRange;
                if (sx < 0) sx = 0;
            }
            if (sightRange > nod.Y)
            {
                sy = 0;
            }
            else
            {
                sy = nod.Y - sightRange;
                if (sy < 0) sy = 0;
            }
            ex = nod.X + sightRange;
            if (ex > realX) ex = (uint)realX;
            ey = nod.Y + sightRange;
            if (ey > realY) ey = (uint)realY;
            for (uint i = sx; i < ex; i++)
            {
                for (uint j = sy; j < ey; j++)
                {
                    if (i >= 0 && i < realX && j >= 0 && j < realY)
                    {
                        if (mapData[i, j] != null)
                        {
                            tmpList.AddRange(mapData[i, j].EntitysIDs);
                        }
                    }
                    else
                    {
                        Output.WriteLine(ConsoleColor.Red, "MapData::GetAllMobsAtRange INDEX OUT OF BOUND " + i.ToString() + "," + j.ToString());
                    }
                }
            }
            HashSet<int> tmHash = new HashSet<int>(tmpList);
            tmpList = new List<int>(tmHash);
            return tmpList;
        }

        public List<int> GetAllPlayersAtTile(uint x, uint y)
        {
            Nod n = new Nod(x, y);
            return GetAllPlayersAtRange(n, 0);
        }

        public List<int> GetAllMobsAtTile(uint x, uint y)
        {
            Nod n = new Nod(x, y);
            return GetAllMobsAtRange(n, 0);
        }

        public bool AddEntityAtTile(Nod nod, int entityID, bool isPlayer)
        {
            if(nod.X > realX || nod.X < 0 || nod.Y > realY || nod.Y < 0)
            {
                Output.WriteLine("MapData::AddEntityAtTile Coordinates out of map!");
                return false;
            }
            if(mapData[nod.X, nod.Y] == null)
            {
                return false;
            }
            if(isPlayer) mapData[nod.X, nod.Y].AddPlayer(entityID);
            else mapData[nod.X, nod.Y].AddEntity(entityID);
            return true;
        }

        public bool RemoveEntityAtTile(Nod nod, int entityID, bool isPlayer)
        {
            if (isPlayer)
            {
                if (mapData[nod.X, nod.Y] != null)
                {
                    mapData[nod.X, nod.Y].RemovePlayer(entityID);
                    return true;
                }
                return false;
            }
            else
            {
                if (mapData[nod.X, nod.Y] != null)
                {
                    mapData[nod.X, nod.Y].RemoveEntity(entityID);
                    return true;
                }
                return false;
            }
        }

        public Bitmap GetWorldImage(Bitmap source)
        {
            for (int i = 0; i < realX; i++)
            {
                for (int j = 0; j < realY; j++)
                {
                    if (mapData[i, j] != null)
                    {
                        if (mapData[i, j].CountEntitys > 0 && mapData[i,j].CountPlayers > 0)
                        {
                            source.SetPixel(i, j, Color.Black);
                        }
                        else
                            if (mapData[i, j].CountEntitys > 0)
                            {
                                source.SetPixel(i, j, Color.Red);
                            }
                            else
                                if (mapData[i, j].CountPlayers > 0)
                                {
                                    source.SetPixel(i, j, Color.Green);
                                }
                    }
                    else
                    {
                        source.SetPixel(i, j, Color.Gray);
                    }
                }
            }
            return source;
        }

        public List<int> DEBUG_GetAllPlayers()
        {
            Nod n = new Nod((uint)(realX / 2), (uint)(realY / 2));
            return GetAllPlayersAtRange(n, (uint)realX);
        }

        public void DEBUG_PlayersAtMap()
        {
            for (int i = 0; i < realX; i++)
            {
                for (int j = 0; j < realY; j++)
                {
                    if (mapData[i, j] != null && mapData[i,j].CountPlayers > 0)
                    {
                        Output.WriteLine("MAP [" + i.ToString() + "," + j.ToString() + "]");
                        for (int k = 0; k < mapData[i, j].PlayersIDs.Length; k++)
                        {
                            Output.WriteLine("   PLAYER ID: " + mapData[i, j].PlayersIDs[k].ToString());
                        }
                    }
                }
            }
        }

    }
}
