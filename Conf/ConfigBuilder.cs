using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cod4.Monitor.Conf
{
    static class ConfigBuilder
    {
        public static Cod4MasterBot.Config Build()
        {
            string temp = String.Empty;
            Cod4MasterBot.Config config = new Cod4MasterBot.Config();
            do
            {
                Console.Write("Network Query Port: ");
                temp = Console.ReadLine()?.Trim();
            } while (!Regex.Match(temp, "^\\d{1,6}$").Success);
            config.Network.QueryPort = Convert.ToInt32(temp);

            do
            {
                Console.Write("Database Hostname/IP: ");
                temp = Console.ReadLine()?.Trim();
            } while (temp == String.Empty);
            config.Database.Host = temp;
            do
            {
                Console.Write("Database User: ");
                temp = Console.ReadLine()?.Trim();
            } while (temp == String.Empty);
            config.Database.User = temp;

            do
            {
                Console.Write("Database User Pass: ");
                temp = Console.ReadLine()?.Trim();
            } while (temp == String.Empty);
            config.Database.Password = temp;

            do
            {
                Console.Write("Database Schema: ");
                temp = Console.ReadLine()?.Trim();
            } while (temp == String.Empty);
            config.Database.DataBase = temp;

            return config;
        }
    }
}
