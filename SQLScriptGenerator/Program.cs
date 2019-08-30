﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLScriptGenerator
{
    class Program
    {
        public static string CONNECTIONSTRING = "";
        public static string SERVER = "";
        public static string DB = "";
        public static string USERNAME = "";
        public static string PASSWORD = "";
        public static string FOLDERNAME = "";
        public static string ITEMS = "";

        static void Main(string[] args)
        {
            Log($"Welcome to SQL Script Generator, fill the following fields: {Environment.NewLine}");

            SERVER = GetString("Server");
            DB = GetString("Database");
            USERNAME = GetString("Username");
            PASSWORD = GetString("Password");
            FOLDERNAME = $"{DB}-{DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss")}";
            ITEMS = GetString("Queries (separated by comma, case sensitive)");

            if (GetString($"Is the data introduced correct? (y/n)").ToLowerInvariant() == "y")
            {
                CONNECTIONSTRING = $"Data Source={SERVER};Initial Catalog={DB};Persist Security Info=True;User ID={USERNAME};Password={PASSWORD};Connect Timeout=60";

                var items = ITEMS.Replace(" ", "").Split(',').ToList();
                List<string> fullQuery = new List<string>();
                foreach (var item in items)
                {
                    try
                    {
                        Log($"Starting with query '{item}'");
                        var strings = GetResultOfFunction(item);
                        Log($"Getting query body");
                        var newStrings = GetListToSave(strings, item);
                        Log($"Appending headers");
                        SaveToFile($"{item}.sql", newStrings);
                        Log($"Saving file '{item}.sql'");
                        fullQuery.AddRange(newStrings);
                        Log($"Finished successfully");
                    }
                    catch (Exception e)
                    {
                        Log($"Exception: {e.Message}");
                    }
                }

                SaveToFile($"allqueries.sql", fullQuery);
                Log($"Saving all queries in file 'allqueries.sql'");
            }

            Log($"Finished, press enter to exit");
            Console.ReadKey();
        }

        private static List<string> GetResultOfFunction(string name)
        {
            var query = $"EXEC sp_helptext '{name}'";

            List<string> result = new List<string>();
            using (SqlConnection mConnection = new SqlConnection(CONNECTIONSTRING))
            {
                mConnection.Open();
                using (SqlCommand cmd = new SqlCommand(query, mConnection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add((string)reader[0]);
                        }
                    }
                }
            }

            return result;
        }

        private static List<string> GetListToSave(List<string> data, string queryname)
        {
            var split = queryname.Split('.');
            var drop = data[1].Contains("FUNCTION") || data[0].Contains("FUNCTION") ? "FUNCTION " : "PROCEDURE";
            var header = $"-- ========================================================================================== {Environment.NewLine}" +
                $"-- This routine has been autogenerated {Environment.NewLine}" +
                $"-- Source code for this tool is at https://github.com/emimontesdeoca/sql-script-generator {Environment.NewLine}" +
                $"-- Source query: {queryname}{Environment.NewLine}" +
                $"-- Create date: {DateTime.Now}{Environment.NewLine}" +
                $"-- Database: {DB}{Environment.NewLine}" +
                $"-- User: {USERNAME}{Environment.NewLine}" +
                $"-- ========================================================================================== {Environment.NewLine}{Environment.NewLine}";
            var template = $"{header} IF EXISTS {Environment.NewLine} (SELECT * FROM Information_schema.Routines R WHERE R.ROUTINE_NAME " +
                $"= '{split[1]}' AND R.ROUTINE_SCHEMA = '{split[0]}') {Environment.NewLine} DROP {drop} [{split[0]}].[{split[1]}] {Environment.NewLine} GO {Environment.NewLine} {Environment.NewLine}";

            data.Insert(0, template);
            data.Add($"{Environment.NewLine}GO{Environment.NewLine}");

            return data;
        }

        private static void SaveToFile(string filename, List<string> data)
        {
            if (!Directory.Exists(FOLDERNAME))
            {
                Directory.CreateDirectory(FOLDERNAME);
            }
            System.IO.File.WriteAllText(Path.Combine(FOLDERNAME, filename), string.Join("", data.ToArray()));
        }

        private static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] - {message}");
        }

        private static string GetString(string text)
        {
            Console.Write($"[{DateTime.Now}] - {text}: ");
            return Console.ReadLine();
        }
    }
}
