using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class UsersLobby
    {
        public class LobbyUser
        {
            int userID;
            int playerID;
            byte[] guid;
            byte startKey;

            public LobbyUser(int userID, int playerID, byte startKey, byte[] guid)
            {
                this.userID = userID;
                this.playerID = playerID;
                this.startKey = startKey;
                this.guid = guid;
            }
            public int UserID { get { return userID; } set { userID = value; } }
            public int PlayerID { get { return playerID; } set { playerID = value; } }
            public byte StartKey { get { return startKey; } set { startKey = value; } }
            public byte[] GUID { get { return guid; } set { guid = value; } }
        }

        static MultiKeyDictionary<LobbyUser> logUser = new MultiKeyDictionary<LobbyUser>();

        public static bool Add(int UID, int PID, byte key, byte[] GUID)
        {
            LobbyUser lUser = new LobbyUser(UID, PID, key, GUID);
            return logUser.Add(UID, lUser);
        }

        public static bool Exists(int UID)
        {
            return logUser.ContainsKey(UID);
        }

        public static void Remove(int UID, int PID)
        {
            logUser.Remove(UID, PID);
        }

        public static void Remove(int UID, out LobbyUser val)
        {
            logUser.Remove(UID, out val);
        }

        internal static void Remove()
        {
            throw new NotImplementedException();
        }
        public static int Count
        {
            get
            {
                return logUser.baseDictionary.Count;
            }
        }
    }
}
