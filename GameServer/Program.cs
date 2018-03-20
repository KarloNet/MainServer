using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class Program
    {
        public static bool DEBUG_recv = false;
        public static bool DEBUG_send = false;
        public static bool DEBUG_Decrypt = false;
        public static bool DEBUG_Encrypt = false;
        public static bool DEBUG_Login_Send = false;
        public static bool DEBUG_Login_Recv = false;
        public static bool DEBUG_Main_Loop = false;
        public static bool DEBUG_MOB_STOP = false;
        //port that is used by LoginServer
        public static int port;
        public static string userIP;
        public static int userPort;
        //max number of active connection
        public static int maxNumberOfConnections;
        //buffer size for recv / send connection
        public static int bufferSize;
        //This is the maximum number of asynchronous accept operations that can be 
        //posted simultaneously. This determines the size of the pool of 
        //SocketAsyncEventArgs objects that do accept operations. Note that this
        //is NOT the same as the maximum number of connections.
        public static int maxSimultaneousAcceptOps;
        //The size of the queue of incoming connections for the listen socket.
        public static int backlog;
        //white list enabled
        public static bool useWhiteList;
        //black list enabled
        public static bool useBlackList;
        //temp black list enabled
        public static bool useTempBlackList;

        public const int receivePrefixLength = 5;
        public const int sendPrefixLength = 5;
        public const int sendHeaderLength = 1;

        public static byte[] hashClientKey;//first key for connection  client - server
        public static byte[] mainKey;//next key for connection client - server
        public static byte[] loginKey;//key for connection with login server
        // This is used only for crypt user data in DB
        // dont hardcode key/salt, force input on server start so it will stay only in memory.
        // for more protection can be xored in memory too ?
        public static string aesKey;
        public static string aesSalt;

        //DB connection string (load from file ? xD) - main Login DB
        public static string dbConnStr;

        public static int maxMessageLength = 255;// server-client maximum size of message (not packet, in one packet can be x messages or one message can be in x packets)

        //time in seconds after connection will be closed if no action from connected client
        public static int maxWaitTime = 300;//5 minutes
        public static bool noDelayConnection = true;//set to true if we want to close inactive connections

        [ThreadStatic]
        public static Random _random = new Random(Environment.TickCount);

        public static Random random
        {
            get
            {
                if (_random == null)
                {
                    _random = new Random(Environment.TickCount);
                }
                return _random;
            }
        }

        static void Main(string[] args)
        {
            Output.SetOut(Output.OutType.Console);//set default output to window console
            //Output.SetOut(Output.OutType.Stream);//set default output to file console
            GameServer gServer = new GameServer();
            if (!gServer.Init())
            {
                //Init server fail, terminate program
                terminate();
            }
            gServer.Start();
            Output.CleanUp();
            Output.WriteLine(ConsoleColor.Yellow, "Game Server closed");
            Output.WriteLine(ConsoleColor.Yellow, "Press any key");
            Output.WaitForKeyPress();
        }

        private static void terminate()
        {
            Output.WriteLine(ConsoleColor.Red, "Game Server terminated!");
            Output.WriteLine(ConsoleColor.Red, "Press any key");
            Output.WaitForKeyPress();
        }
    }
}
