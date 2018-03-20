using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Drawing;

namespace GameServer
{
    class GameServer
    {
        LoginConnectionListener loginListener;
        UserConnectionListener userListener;
        SocketListenerSettings userSocketSettings;
        SocketListenerSettings loginSocketSettings;
        GameServerMainLoop mainServerLoop;
        Thread mainLoopThread;
        public static World world;

        byte[] hashClientKey;
        byte[] mainKey;
        byte[] loginKey;

        //These strings are for output interraction.
        const string info = "/INFO";  //show current server information ( number of connections / threads / pools ect..)
        const string info_player = "/INFO PLAYER";  //show current server information ( number of connections / threads / pools ect..)
        const string info_monster = "/INFO MOB";  //show current server information ( number of connections / threads / pools ect..)
        const string info_map = "/INFO PLAYER MAP";  //show current server information ( number of connections / threads / pools ect..)
        const string info_pos = "/INFO PLAYER POSITION";  //show current server information ( number of connections / threads / pools ect..)
        const string closeString = "/CLOSE";  //shutdown login server
        const string debug = "/DEBUG";  //show debug messages in console
        const string debugUser = "/DEBUG USER";  //show debug messages in console
        const string debugUserRecv = "/DEBUG USER_RECV";  //show debug messages in console
        const string debugUserSend = "/DEBUG USER_SEND";  //show debug messages in console
        const string debugGameServer = "/DEBUG LOGINSERVER";  //show debug messages in console
        const string debugGameServerRecv = "/DEBUG LOGINSERVER_RECV";  //show debug messages in console
        const string debugGameServerSend = "/DEBUG LOGINSERVER_SEND";  //show debug messages in console
        const string debugDecrypt = "/DEBUG DECRYPT";  //show debug messages in console
        const string debugEncrypt = "/DEBUG ENCRYPT";  //show debug messages in console
        const string clear = "/CLS";  //clear console window
        const string helpString = "/HELP";  //show commands
        //temporary commands?
        const string sendToAll = "/SENDA";    //send packet to all connected clients ( for test only)
        const string testString = "/TEST";   //tests
        const string send = "/SEND"; //send test
        
        public GameServer()
        {
            IniFile configServerFile = new IniFile("Config.ini");
            Program.userIP = configServerFile.GetValue("USER_CONNECTION", "userIP", "127.0.0.1");
            Program.userPort = Program.port = configServerFile.GetInteger("USER_CONNECTION", "port", 3001);
            Program.maxNumberOfConnections = configServerFile.GetInteger("USER_CONNECTION", "maxNumberOfConnections", 1000);
            Program.bufferSize = configServerFile.GetInteger("USER_CONNECTION", "bufferSize", 100);
            Program.backlog = configServerFile.GetInteger("USER_CONNECTION", "backlog", 100);
            Program.useTempBlackList = configServerFile.GetBoolean("RESTRICTION", "tempBlackList", true);
            int port = configServerFile.GetInteger("LOGIN_SERVER", "port", 4422);
            int maxNumberOfConnections = configServerFile.GetInteger("LOGIN_SERVER", "maxNumberOfConnections", 1);
            int bufferSize = configServerFile.GetInteger("LOGIN_SERVER", "bufferSize", 1024);
            int backlog = configServerFile.GetInteger("LOGIN_SERVER", "backlog", 1);
            string key = configServerFile.GetValue("LOGIN_SERVER", "key");
            if (key.Length > 0) loginKey = Encoding.ASCII.GetBytes(key);
            IPEndPoint userLocalEndPoint = new IPEndPoint(IPAddress.Any, Program.port);
            IPEndPoint loginLocalEndPoint = new IPEndPoint(IPAddress.Any, port);
            userSocketSettings = new SocketListenerSettings(Program.maxNumberOfConnections, Program.backlog, Program.receivePrefixLength, Program.bufferSize, Program.sendPrefixLength, userLocalEndPoint);
            loginSocketSettings = new SocketListenerSettings(maxNumberOfConnections, backlog, Program.receivePrefixLength, bufferSize, Program.sendPrefixLength, loginLocalEndPoint);
            Output.WriteLine(ConsoleColor.Green, "GameServer config. Port: " + Program.port.ToString() + " Max connections: " + Program.maxNumberOfConnections.ToString() + " Buffer size: " + Program.bufferSize.ToString());
            loginListener = new LoginConnectionListener(Program.bufferSize, loginSocketSettings);
            userListener = new UserConnectionListener(userSocketSettings);
            world = new World("maps//map_200x200");
            mainServerLoop = new GameServerMainLoop();
        }

        public bool Init()
        {
            MD5 md5 = MD5.Create();
            using (var stream = File.OpenRead("ClientMD5"))
            {
                md5.ComputeHash(stream);
            }
            hashClientKey = md5.Hash;
            Output.WriteLine("Computed hash is: " + ByteArrayToHex(hashClientKey));
            //read on program start DO NOT HARD CODE IT!
            mainKey = Encoding.ASCII.GetBytes("xSd#4%25*Be#sI(8L6Hg$f18jGt9-lN5F6H43sRgB&8#dG6J9!fjmN7Yhj#2");//60 symbols
            Crypt.Xor.Key = mainKey;
            Program.hashClientKey = hashClientKey;
            Program.mainKey = mainKey;
            //key for init communication with login server
            if(loginKey != null)
            {
                Program.loginKey = loginKey;
            }
            else
            {
                Program.loginKey = mainKey;
            }
            //read on program start DO NOT HARD CODE IT!
            Program.aesKey = "SDFT57G@57G$%23&$23B^H%6GU0B-GH5S654D76F3^e54y34546w9vqe54YV%";
            Program.aesSalt = "asdr56HY^dsft*%T";
            //read on program start DO NOT HARD CODE IT!
            Program.dbConnStr = "Data Source = 192.168.137.1\\SQLSERVER; Initial Catalog = TEST_SERVER; User ID = G_TEST; Password = TesT!#AppNew$*";

            //force static constructor to execute
            Skill.SkillHandler.Init();

            if (!loginListener.Init()) return false;
            if (!userListener.Init()) return false;
            if (!world.Init()) return false;
            /*
            int dmg = 0;
            int fromInt;
            int fromWiz = 2;
            int minDmg = 0;
            int maxDmg = 0;
            //test
            for (int i = 10; i < 100; i += 5)
            {
                fromInt = i / 2;
                minDmg = 100 + (((fromInt + fromWiz) * 1) * 5);
                maxDmg = 300 + (((fromInt + fromWiz) * 1) * 5);
                Output.WriteLine(ConsoleColor.DarkYellow, "Action::CalculateMagicDmg Att int: " + i.ToString() + " wiz: 16  Skill lvl: 1  DMG " + minDmg.ToString() + "-" + maxDmg.ToString());
            }
            Output.WriteLine("NOW WE GOT LVL UP SKILL xD");
            for (int i = 10; i < 100; i += 5)
            {
                fromInt = i / 2;
                minDmg = 100 + (((fromInt + fromWiz) * 2) * 5);
                maxDmg = 300 + (((fromInt + fromWiz) * 2) * 5);
                Output.WriteLine(ConsoleColor.DarkYellow, "Action::CalculateMagicDmg Att int: " + i.ToString() + " wiz: 16 Skill lvl: 2  DMG " + minDmg.ToString() + "-" + maxDmg.ToString());
            }
            */
            return true;
        }

        public void Start()
        {
            try
            {
                mainLoopThread = new Thread(() => mainServerLoop.Run());
                mainLoopThread.Priority = ThreadPriority.Normal;
                mainLoopThread.Start();

                loginListener.StartListing();
                userListener.StartListing();
                ManageClosing();
            }
            catch (Exception e)
            {
                Output.WriteLine(ConsoleColor.Red, e.ToString());
            }
            CleanUpOnExit();
        }

        void CleanUpOnExit()
        {
            mainServerLoop.stopMainLoop = true;//stop main game loop
            userListener.CleanUpOnExit();
            loginListener.CleanUpOnExit();
        }

        void ManageClosing()
        {
            string stringToCompare = "";
            string theEntry = "";
            string entry = "";
            string entryData = "";

            while (stringToCompare != closeString)
            {
                entry = Output.ReadLine();
                if (entry.IndexOf(" ") > 0)
                {
                    theEntry = entry.Substring(0, entry.IndexOf(" ")).ToUpper();
                    if (entry.IndexOf(" ") + 1 < entry.Length)
                    {
                        entryData = entry.Substring(entry.IndexOf(" ") + 1).ToUpper();
                    }
                    else
                    {
                        entryData = "";
                    }
                }
                else
                {
                    theEntry = entry.ToUpper();
                    entryData = "";
                }

                switch (theEntry)
                {
                    case send:
                        break;
                    case clear:
                        Output.Clear();
                        break;
                    case debug:
                        switch (entryData)
                        {
                            case "":
                                if (Program.DEBUG_Decrypt || Program.DEBUG_Encrypt || Program.DEBUG_Login_Send || Program.DEBUG_Login_Recv || Program.DEBUG_send || Program.DEBUG_recv)
                                {
                                    Program.DEBUG_recv = false;
                                    Program.DEBUG_send = false;
                                    Program.DEBUG_Login_Recv = false;
                                    Program.DEBUG_Login_Send = false;
                                    Program.DEBUG_Decrypt = false;
                                    Program.DEBUG_Encrypt = false;
                                    Output.WriteLine("DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("DEBUG MOD ON");
                                    Program.DEBUG_recv = true;
                                    Program.DEBUG_send = true;
                                    Program.DEBUG_Login_Recv = true;
                                    Program.DEBUG_Login_Send = true;
                                    Program.DEBUG_Decrypt = true;
                                    Program.DEBUG_Encrypt = true;
                                }
                                break;
                            case "USER":
                                if (Program.DEBUG_recv || Program.DEBUG_send)
                                {
                                    Program.DEBUG_recv = false;
                                    Program.DEBUG_send = false;
                                    Output.WriteLine("USER DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("USER DEBUG MOD ON");
                                    Program.DEBUG_recv = true;
                                    Program.DEBUG_send = true;
                                }
                                break;
                            case "USER_RECV":
                                if (Program.DEBUG_recv)
                                {
                                    Program.DEBUG_recv = false;
                                    Output.WriteLine("USER RECV DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("USER RECV DEBUG MOD ON");
                                    Program.DEBUG_recv = true;
                                }
                                break;
                            case "USER_SEND":
                                if (Program.DEBUG_send)
                                {
                                    Program.DEBUG_send = false;
                                    Output.WriteLine("USER SEND DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("USER SEND DEBUG MOD ON");
                                    Program.DEBUG_send = true;
                                }
                                break;
                            case "LOGINSERVER":
                                if (Program.DEBUG_Login_Recv || Program.DEBUG_Login_Send)
                                {
                                    Program.DEBUG_Login_Recv = false;
                                    Program.DEBUG_Login_Send = false;
                                    Output.WriteLine("LOGIN SERVER DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("LOGIN SERVER DEBUG MOD ON");
                                    Program.DEBUG_Login_Recv = true;
                                    Program.DEBUG_Login_Send = true;
                                }
                                break;
                            case "LOGINSERVER_RECV":
                                if (Program.DEBUG_Login_Recv)
                                {
                                    Program.DEBUG_Login_Recv = false;
                                    Output.WriteLine("LOGIN SERVER RECV DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("LOGIN SERVER RECV DEBUG MOD ON");
                                    Program.DEBUG_Login_Recv = true;
                                }
                                break;
                            case "LOGINSERVER_SEND":
                                if (Program.DEBUG_Login_Send)
                                {
                                    Program.DEBUG_Login_Send = false;
                                    Output.WriteLine("LOGIN SERVER SEND DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("LOGIN SERVER SEND DEBUG MOD ON");
                                    Program.DEBUG_Login_Send = true;
                                }
                                break;
                            case "DECRYPT":
                                if (Program.DEBUG_Decrypt)
                                {
                                    Program.DEBUG_Decrypt = false;
                                    Output.WriteLine("DECRYPT DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("DECRYPT DEBUG MOD ON");
                                    Program.DEBUG_Decrypt = true;
                                }
                                break;
                            case "ENCRYPT":
                                if (Program.DEBUG_Encrypt)
                                {
                                    Program.DEBUG_Encrypt = false;
                                    Output.WriteLine("ENCRYPT DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("ENCRYPT DEBUG MOD ON");
                                    Program.DEBUG_Encrypt = true;
                                }
                                break;
                            case "MAIN LOOP":
                                if (Program.DEBUG_Main_Loop)
                                {
                                    Program.DEBUG_Main_Loop = false;
                                    Output.WriteLine("Main Loop DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("Main Loop DEBUG MOD ON");
                                    Program.DEBUG_Main_Loop = true;
                                }
                                break;
                            case "MOB STOP":
                                if (Program.DEBUG_MOB_STOP)
                                {
                                    Program.DEBUG_MOB_STOP = false;
                                    Output.WriteLine("Mob Stop DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("Mob Stop DEBUG MOD ON");
                                    Program.DEBUG_MOB_STOP = true;
                                }
                                break;
                            default:
                                Output.WriteLine("WRONG IN COMMAND DATA");
                                break;
                        }
                        break;
                    case info:
                        switch (entryData)
                        {
                            case "":
                                Output.WriteLine("Number of active connections = " + userListener.ConnectionCount.ToString() + " Active in pool: " + userListener.ActiveConnInPool.ToString() + " and Inactive: " + userListener.InactiveConnInPool.ToString());
                                break;
                            case "PLAYER":
                                Output.WriteLine("Number of player in game = " + world.PlayerCount.ToString());
                                Output.WriteLine("Number of player waiting to login = " + UsersLobby.Count.ToString());
                                break;
                            case "MOB":
                                Output.WriteLine("Number of monsters in game = " + world.MonstersCount.ToString());
                                foreach( Database.Mob m in world.monsters.Values)
                                {
                                    Output.WriteLine("MOB ID: " + m.InternalID.ToString() + " AT [" + m.PosX.ToString() + "," + m.PosY.ToString() + "]");
                                }
                                break;
                            case "WORLD SAVE":
                                Output.WriteLine("Saving world image");
                                Bitmap bmp = new Bitmap(world.RealWorldX, world.RealWorldY);
                                using (Graphics graph = Graphics.FromImage(bmp))
                                {
                                    Rectangle ImageSize = new Rectangle(0, 0, world.RealWorldX, world.RealWorldY);
                                    graph.FillRectangle(Brushes.White, ImageSize);
                                }
                                bmp = world.GetWorldImage(bmp);
                                bmp.Save("save//map_"+ Environment.TickCount.ToString() +".bmp");
                                Output.WriteLine("World image saved");
                                break;
                            case "PLAYER MAP":
                                List<int> playersAtMap = world.DEBUG_GetAllPlayersAtMap();
                                Output.WriteLine("Players in map:");
                                foreach ( int i in playersAtMap)
                                {
                                    Output.WriteLine("ID = " + i.ToString());
                                }
                                Output.WriteLine("End of list");
                                break;
                            case "PLAYER POSITION":
                                Output.WriteLine("Players position:");
                                foreach (Database.Player p in world.players.Values)
                                {
                                    Output.WriteLine("PLAYER: " + p.PlayerPID.ToString() + " Name: " + p.PlayerName + " Position [" + p.PosX.ToString() + "," + p.PosY.ToString() + "," + p.PosZ.ToString() + "] at map [" + p.MapPosition.X.ToString() + "," + p.MapPosition.Y.ToString() + "]");
                                }
                                Output.WriteLine("End of player list");
                                break;
                            case "PLAYER POSITION MAP":
                                Output.WriteLine("Players position at map:");
                                world.DEBUG_PlayersAtMap();
                                Output.WriteLine("End of player list");
                                break;
                            default:
                                Output.WriteLine("Unrecognized command");
                                break;
                        }
                        break;
                    case closeString:
                        stringToCompare = closeString;
                        break;
                    case testString:
                        Output.SetOut(Output.OutType.Console);//set default output to window console
                        break;
                    case helpString:
                        Output.WriteLine("Commands:");
                        Output.WriteLine("/help - show commands");
                        Output.WriteLine("/close - close server");
                        Output.WriteLine("info - some info about server");
                        Output.WriteLine("/cls - clear output window");
                        Output.WriteLine("/debug - show debug info");
                        break;
                    default:
                        Output.WriteLine("Unrecognized command");
                        break;
                }
            }
        }

        public static string ByteArrayToHex(byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];
            byte b;
            for (int i = 0; i < bytes.Length; i++)
            {
                b = ((byte)(bytes[i] >> 4));
                c[i * 2] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = ((byte)(bytes[i] & 0xF));
                c[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(c);
        }

        public static bool ByteArrayCompare(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
                if (a1[i] != a2[i])
                    return false;

            return true;
        }
    }
}
