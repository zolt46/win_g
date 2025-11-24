// File: PublicPCControl.Client/Data/DatabaseInitializer.cs
using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace PublicPCControl.Client.Data
{
    public static class DatabaseInitializer
    {
        public static string GetConnectionString()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbPath = Path.Combine(localAppData, "PublicPCControl", "Data", "publicpc.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            return new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        }

        public static void EnsureDatabase()
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();

            var commands = new[]
            {
                @"CREATE TABLE IF NOT EXISTS sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    pc_name TEXT,
                    user_name TEXT,
                    user_id TEXT,
                    purpose TEXT,
                    start_time TEXT,
                    end_time TEXT,
                    requested_minutes INTEGER,
                    end_reason TEXT
                );",
                @"CREATE TABLE IF NOT EXISTS process_logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    session_id INTEGER,
                    process_name TEXT,
                    executable_path TEXT,
                    started_at TEXT,
                    ended_at TEXT,
                    end_reason TEXT
                );",
                @"CREATE TABLE IF NOT EXISTS window_logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    session_id INTEGER,
                    process_name TEXT,
                    window_title TEXT,
                    changed_at TEXT
                );",
                @"CREATE TABLE IF NOT EXISTS allowed_programs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    display_name TEXT,
                    executable_path TEXT,
                    arguments TEXT
                );"
            };

            foreach (var sql in commands)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
    }
}