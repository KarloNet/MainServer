using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    static class InGameUsers
    {
        public class GameUser
        {
            int userID;
            int playerID;
            Connection con;

            public GameUser(int userID, int playerID, Connection connection)
            {
                this.userID = userID;
                this.playerID = playerID;
                this.con = connection;
            }
            public int UserID { get { return userID; } set { userID = value; } }
            public int PlayerID { get { return playerID; } set { playerID = value; } }
            public Connection UserConnection { get { return con; } set { con = value; } }
        }

        static MultiKeyDictionary<GameUser> gameUser = new MultiKeyDictionary<GameUser>();

        public static bool Add(int UID, int playerID, Connection userCon)
        {
            GameUser gUser = new GameUser(UID, playerID, userCon);
            return gameUser.Add(UID, playerID, gUser);
        }

        public static bool Exists(int UID)
        {
            return gameUser.ContainsKey(UID);
        }

        public static void Remove(int UID, int connectionToken)
        {
            gameUser.Remove(UID, connectionToken);
        }

        public static void Remove(int UID, out GameUser val)
        {
            gameUser.Remove(UID, out val);
        }

        public static int[] GetCurrentUidList()
        {
            return gameUser.baseDictionary.Keys.ToArray();
        }

        public static int Count
        {
            get
            {
                return gameUser.baseDictionary.Count;
            }
        }
    }
}
