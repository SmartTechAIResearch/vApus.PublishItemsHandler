/*
 * Copyright 2016 (c) Sizing Servers Lab
 * University College of West-Flanders, Department GKG
 * 
 * Author(s):
 *    Dieter Vandroemme
 */
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using vApus.Util;

namespace vApus.PublishItemsHandler {
    class Program {
        private static Properties.Settings _settings = Properties.Settings.Default;

        private static readonly Mutex _namedMutex = new Mutex(true, Assembly.GetExecutingAssembly().FullName);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);


        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOACTIVATE = 0x0010;

        static void Main(string[] args) {
            string title = "vApus publish items handler";
            if (_namedMutex.WaitOne(0)) {
                int port = 4337;

                Console.Title = title;

                Console.WriteLine("vApus publish items handler");
                Console.WriteLine("-----");
                Console.WriteLine("This handler listens for messages on TCP port " + port +
                    " over IPv4 published by vApus and puts them in a standardized vApus MySQL results database.");

                try {
                    Console.WriteLine();

                    object[] credentials = ReadCredentials();
                    PublishItemHandler.Init(credentials[0] as string, (int)credentials[1], credentials[2] as string, credentials[3] as string);
                    Console.WriteLine("Connected to MySQL " + credentials[2] + "@" + credentials[0] + ":" + credentials[1]);
                    QueuedListener.Start(port);
                    Console.WriteLine("Listening for incoming messages...");

                    Console.WriteLine();
                }
                catch (Exception ex) {
                    Console.WriteLine("Failed connecting to MySQL\n" + ex.Message);
                    RemoveCredentials();
                }

                if (args.Length != 0 && args[0] == "autohide")
                    SetWindowPos(FindWindowByCaption(IntPtr.Zero, title), HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

                Console.WriteLine("Press <any key> to quit");
                Console.ReadKey();

                if (_settings.MySQLHost.Length != 0 && _settings.MySQLUser.Length != 0) {
                    Console.WriteLine("Do you want to remove your stored credentials? (y or n)");
                    if (Console.ReadLine().Trim().ToLowerInvariant() == "y")
                        RemoveCredentials();
                }
            }
        }

        private static object[] ReadCredentials() {
            if (_settings.MySQLHost.Length == 0 || _settings.MySQLUser.Length == 0) {
                Console.WriteLine("Type <MySQL host>,<port>,<user> and press enter");

                string[] arr = Console.ReadLine().Split(',');

                Console.WriteLine("Type <password> and press enter");

                string password = ReadPassword();

                var credentials = new object[4];
                credentials[0] = arr[0];
                credentials[1] = int.Parse(arr[1]);
                credentials[2] = arr[2];
                credentials[3] = password;

                Console.WriteLine("Do you want to store these credentials for next time? (y or n)");

                if (Console.ReadLine().Trim().ToLowerInvariant() == "y") {
                    _settings.MySQLHost = arr[0];
                    _settings.MySQLPort = (int)credentials[1];
                    _settings.MySQLUser = arr[2];
                    _settings.MySQLPassword = password.Encrypt("{B4AB09F7-407F-473E-A038-C49F21B193E8}", new byte[] { 0x95, 0x16, 0x39, 0x3f, 0xa1, 0x4c, 0xc5, 0x54, 0x76, 0x45, 0x10, 0x11, 0x22 });

                    _settings.Save();
                }

                return credentials;
            }
            else {
                return new object[] {
                _settings.MySQLHost,
                _settings.MySQLPort,
                _settings.MySQLUser,
                _settings.MySQLPassword.Decrypt("{B4AB09F7-407F-473E-A038-C49F21B193E8}", new byte[] { 0x95, 0x16, 0x39, 0x3f, 0xa1, 0x4c, 0xc5, 0x54, 0x76, 0x45, 0x10, 0x11, 0x22 })
                };
            }
        }

        private static void RemoveCredentials() {
            _settings.MySQLHost = string.Empty;
            _settings.MySQLPort = 3306;
            _settings.MySQLUser = string.Empty;
            _settings.MySQLPassword = string.Empty;

            _settings.Save();
        }

        private static string ReadPassword() {
            string password = string.Empty;
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter) {
                if (info.Key != ConsoleKey.Backspace) {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace) {
                    if (!string.IsNullOrEmpty(password)) {
                        password = password.Substring(0, password.Length - 1);
                        int pos = Console.CursorLeft;
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }
                info = Console.ReadKey(true);
            }
            Console.WriteLine();
            return password;
        }
    }
}

