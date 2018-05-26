using System;
using System.IO;
using System.Text.RegularExpressions;
using Cod4.Monitor.Conf;
using Cod4MasterBot;
using Newtonsoft.Json;

namespace Cod4.Monitor.Util
{
    static class Common
    {
        public static void FatalError(string error,string message)
        {
            ColourWrite($"^7[^1Fatal Error^7] {error}\n\n->{message}.");
            WaitForExit();
        }
        private static void WaitForExit()
        {
            Console.Read();
            Environment.Exit(1);
        }
        public static void ColourWrite(string s)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.White;
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == '^')
                    {
                        switch (s[i + 1])
                        {
                            case '1': Console.ForegroundColor = ConsoleColor.Red; break;
                            case '2': Console.ForegroundColor = ConsoleColor.Green; break;
                            case '3': Console.ForegroundColor = ConsoleColor.Yellow; break;
                            case '4': Console.ForegroundColor = ConsoleColor.Blue; break;
                            case '5': Console.ForegroundColor = ConsoleColor.Cyan; break;
                            case '6': Console.ForegroundColor = ConsoleColor.Magenta; break;
                            case '7': Console.ForegroundColor = ConsoleColor.White; break;
                            case '8': Console.ForegroundColor = ConsoleColor.Gray; break;
                            case '9': Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                            case '0': Console.ForegroundColor = ConsoleColor.Black; break;
                        }
                        i += 2;
                    }
                    Console.Write(s[i]);
                }
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static Config GetConfig(string configfileName)
        {
            Config config = null;
            if (File.Exists(configfileName))
            {
                try
                {
                    Console.WriteLine("Reading Config..");
                    config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configfileName));
                    Console.WriteLine("Config Loaded");
                }
                catch (Exception e) { Common.FatalError("Error While Loading Config", e.Message); }
            }
            else
            {
                try
                {
                    Console.WriteLine($"{configfileName} not found..\n<Build New>");
                    config = ConfigBuilder.Build();
                    Console.WriteLine("Saving Config..");
                    File.WriteAllText(configfileName, JsonConvert.SerializeObject(config));
                    Console.WriteLine("Config Saved");

                }
                catch (Exception e)
                { Common.FatalError("Error While Building Config", e.Message); }
            }
            return config;
        }
        public static string RemoveColours(string s)
        {
            return Regex.Replace(s, @"\^\d", "");
        }
    }
}
