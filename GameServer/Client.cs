using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class Client
    {
        public enum STATUS
        {
            Connected = 0x01,
            Login = 0x02
        }

        public enum DECODE_TYPE
        {
            XOR = 1134,//xor with key > 1 and xor one after ( default one )
            AES = 1263,//standard aes with aes key and salt
            BXO = 1080,//xor using only one byte key
            COD = 1156 //decode using decode table and key
        }

        public enum ENCODE_TYPE
        {
            XOR = 3411,//xor with key > 1 and xor one after ( default one )
            AES = 6312,//standard aes with aes key and salt
            BXO = 8010,//xor using only one byte key
            COD = 5611 //encode using encode table and key
        }

        byte[] privateKey;
        int recvKeyOffset;
        int sendKeyOffset;
        public byte recvKeyCOD = 0;
        public byte sendKeyCOD = 0;
        STATUS status;
        int numberOfLoginTry;
        int userID;
        //List<Database.Player> playerList;
        Database.Player player;
        DECODE_TYPE decodeType;
        ENCODE_TYPE encodeType;
        Connection con;
        World myWorld;

        public int RecvKeyOffset { get { return recvKeyOffset; } set { recvKeyOffset = value; if (recvKeyOffset > privateKey.Length || recvKeyOffset < 0) recvKeyOffset = 0; } }
        public int SendKeyOffset { get { return sendKeyOffset; } set { sendKeyOffset = value; if (sendKeyOffset > privateKey.Length || sendKeyOffset < 0) sendKeyOffset = 0; } }
        public DECODE_TYPE DecodeType { get { return decodeType; } set { decodeType = value; } }
        public ENCODE_TYPE EncodeType { get { return encodeType; } set { encodeType = value; } }
        public World CurrentWorld { get { return myWorld; } set { myWorld = value; } }

        public Client(byte[] privateKey, Connection userConnection)
        {
            this.privateKey = privateKey;
            this.status = STATUS.Connected;
            this.recvKeyOffset = 0;
            this.sendKeyOffset = 0;
            DecodeType = DECODE_TYPE.XOR;
            EncodeType = ENCODE_TYPE.XOR;
            this.numberOfLoginTry = 0;
            userID = -1;
            myWorld = null;
            con = userConnection;
            //player = new Database.Player(con);
        }

        public STATUS Status
        {
            get
            {
                return this.status;
            }
            set
            {
                this.status = value;
            }
        }

        public int NumberOfLoginTrys
        {
            get
            {
                return this.numberOfLoginTry;
            }
            set
            {
                this.numberOfLoginTry = value;
            }
        }

        public byte[] PrivateKey
        {
            get
            {
                return this.privateKey;
            }
            set
            {
                this.privateKey = value;
            }
        }

        public int UserID
        {
            get { return this.userID; }
            set { this.userID = value; }
        }

        public void SetPlayer(Database.Player player)
        {
            this.player = player;
        }

        public Database.Player GetPlayer()
        {
            return this.player;
        }

        public void DeletePlayer()
        {
            this.player = null;
        }

        public int PlayerID
        {
            get
            {
                if (this.player != null)
                {
                    return this.player.PlayerPID;
                }
                else
                {
                    return -1;
                }
            }
            set
            {
                if (this.player != null)
                {
                    this.player.PlayerPID = value;
                }
                else
                {
                    this.player = new Database.Player(con);
                    this.player.PlayerPID = value;
                }
            }
        } 

    }
}
