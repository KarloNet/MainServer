using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace GameServer
{
    static class Output
    {
        public enum OutType
        {
            Console,
            Window,
            Stream
        }
        //for avoid console freezes antil key inout receive, DON'T WORK ON WIN 10 !!!!..... :/
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        private static OutType outType = OutType.Console;
        private static System.IO.StreamWriter file;

        public static void SetOut(OutType type)
        {
            outType = type;
            switch (outType)
            {
                case OutType.Console:
                    IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
                    SetConsoleMode(handle, ENABLE_EXTENDED_FLAGS);
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    file = new System.IO.StreamWriter(@"debugOut.txt", true);//@"C:\Users\Public\TestFolder\WriteLines2.txt"
                    file.WriteLine("");
                    file.WriteLine("START OF LOG " + DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString());
                    break;
                default:
                    IntPtr handle2 = Process.GetCurrentProcess().MainWindowHandle;
                    SetConsoleMode(handle2, ENABLE_EXTENDED_FLAGS);
                    break;
            }
        }

        public static void Write(ConsoleColor color, string text)
        {
            switch (outType)
            {
                case OutType.Console:
                    Console.ForegroundColor = color;
                    Console.Write(text);
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    if (file != null)
                    {
                        file.Write(text);
                    }
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(text);
                    break;
            }
        }

        public static void Write(string text)
        {
            Write(ConsoleColor.White, text);
        }

        public static void WriteLine(ConsoleColor color, string text)
        {
            switch (outType)
            {
                case OutType.Console:
                    Console.ForegroundColor = color;
                    Console.WriteLine(text);
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    if (file != null)
                    {
                        file.WriteLine(text);
                    }
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(text);
                    break;
            }
        }

        public static void WriteLine(string text)
        {
            WriteLine(ConsoleColor.White, text);
        }


        private static void ResetColor()
        {
            switch (outType)
            {
                case OutType.Console:
                    Console.ResetColor();
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    break;
                default:
                    Console.ResetColor();
                    break;
            }
        }

        public static void WaitForKeyPress()
        {
            switch (outType)
            {
                case OutType.Console:
                    Console.ReadKey();
                    break;
                case OutType.Window:
                    Thread.Sleep(4000);
                    break;
                case OutType.Stream:
                    Thread.Sleep(4000);
                    break;
                default:
                    Console.ReadKey();
                    break;
            }
        }

        public static string ReadLine()
        {
            string input = "";
            switch (outType)
            {
                case OutType.Console:
                    input = Console.ReadLine();
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    input = Console.ReadLine();
                    break;
                default:
                    input = Console.ReadLine();
                    break;
            }
            return input;
        }

        public static void Clear()
        {
            switch (outType)
            {
                case OutType.Console:
                    Console.Clear();
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    break;
                default:
                    Console.Clear();
                    break;
            }
        }

        public static void CleanUp()
        {
            // One time only destructor.
            switch (outType)
            {
                case OutType.Console:
                    break;
                case OutType.Window:
                    break;
                case OutType.Stream:
                    if (file != null)
                    {
                        file.WriteLine("END OF LOG " + DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString());
                        file.Flush();
                        file.Close();
                        file = null;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
