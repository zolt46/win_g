// File: PublicPCControl.Client/Data/LogRepository.cs
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.Data
{
    public class LogRepository
    {
        private readonly string _connectionString;

        public LogRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InsertProcessLog(ProcessLog log)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO process_logs(session_id, process_name, executable_path, started_at, ended_at, end_reason)
                               VALUES($sid, $name, $path, $start, $end, $reason);";
            cmd.Parameters.AddWithValue("$sid", log.SessionId);
            cmd.Parameters.AddWithValue("$name", log.ProcessName);
            cmd.Parameters.AddWithValue("$path", log.ExecutablePath);
            cmd.Parameters.AddWithValue("$start", log.StartedAt);
            cmd.Parameters.AddWithValue("$end", (object?)log.EndedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$reason", log.EndReason);
            cmd.ExecuteNonQuery();
        }

        public void InsertWindowLog(WindowLog log)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO window_logs(session_id, process_name, window_title, changed_at)
                               VALUES($sid, $name, $title, $at);";
            cmd.Parameters.AddWithValue("$sid", log.SessionId);
            cmd.Parameters.AddWithValue("$name", log.ProcessName);
            cmd.Parameters.AddWithValue("$title", log.WindowTitle);
            cmd.Parameters.AddWithValue("$at", log.ChangedAt);
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<ProcessLog> GetProcessLogs(DateTime from, DateTime to)
        {
            var list = new List<ProcessLog>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM process_logs WHERE started_at BETWEEN $from AND $to ORDER BY started_at DESC";
            cmd.Parameters.AddWithValue("$from", from);
            cmd.Parameters.AddWithValue("$to", to);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ProcessLog
                {
                    Id = reader.GetInt32(0),
                    SessionId = reader.GetInt32(1),
                    ProcessName = reader.GetString(2),
                    ExecutablePath = reader.GetString(3),
                    StartedAt = reader.GetDateTime(4),
                    EndedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    EndReason = reader.GetString(6)
                });
            }
            return list;
        }

        public IEnumerable<WindowLog> GetWindowLogs(DateTime from, DateTime to)
        {
            var list = new List<WindowLog>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM window_logs WHERE changed_at BETWEEN $from AND $to ORDER BY changed_at DESC";
            cmd.Parameters.AddWithValue("$from", from);
            cmd.Parameters.AddWithValue("$to", to);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new WindowLog
                {
                    Id = reader.GetInt32(0),
                    SessionId = reader.GetInt32(1),
                    ProcessName = reader.GetString(2),
                    WindowTitle = reader.GetString(3),
                    ChangedAt = reader.GetDateTime(4)
                });
            }
            return list;
        }
    }
}