// File: PublicPCControl.Client/Data/SessionRepository.cs
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.Data
{
    public class SessionRepository
    {
        private readonly string _connectionString;

        public SessionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int Insert(Session session)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO sessions(pc_name, user_name, user_id, purpose, start_time, end_time, requested_minutes, max_extensions, extensions_used, extension_minutes, end_reason)
                               VALUES($pc, $name, $id, $purpose, $start, $end, $req, $maxExt, $usedExt, $extMinutes, $reason);
                               SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$pc", session.PcName);
            cmd.Parameters.AddWithValue("$name", session.UserName);
            cmd.Parameters.AddWithValue("$id", session.UserId);
            cmd.Parameters.AddWithValue("$purpose", session.Purpose);
            cmd.Parameters.AddWithValue("$start", session.StartTime);
            cmd.Parameters.AddWithValue("$end", (object?)session.EndTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$req", session.RequestedMinutes);
            cmd.Parameters.AddWithValue("$maxExt", session.MaxExtensions);
            cmd.Parameters.AddWithValue("$usedExt", session.ExtensionsUsed);
            cmd.Parameters.AddWithValue("$extMinutes", session.ExtensionMinutes);
            cmd.Parameters.AddWithValue("$reason", session.EndReason);
            var id = (long)cmd.ExecuteScalar()!;
            return (int)id;
        }

        public void Update(Session session)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"UPDATE sessions SET end_time=$end, end_reason=$reason, requested_minutes=$req, max_extensions=$maxExt, extensions_used=$usedExt, extension_minutes=$extMinutes WHERE id=$id";
            cmd.Parameters.AddWithValue("$end", (object?)session.EndTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$reason", session.EndReason);
            cmd.Parameters.AddWithValue("$req", session.RequestedMinutes);
            cmd.Parameters.AddWithValue("$maxExt", session.MaxExtensions);
            cmd.Parameters.AddWithValue("$usedExt", session.ExtensionsUsed);
            cmd.Parameters.AddWithValue("$extMinutes", session.ExtensionMinutes);
            cmd.Parameters.AddWithValue("$id", session.Id);
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<Session> GetRecent(int count = 100)
        {
            var result = new List<Session>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM sessions ORDER BY start_time DESC LIMIT {count}";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Session
                {
                    Id = reader.GetInt32(0),
                    PcName = reader.GetString(1),
                    UserName = reader.GetString(2),
                    UserId = reader.GetString(3),
                    Purpose = reader.GetString(4),
                    StartTime = reader.GetDateTime(5),
                    EndTime = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    RequestedMinutes = reader.GetInt32(7),
                    MaxExtensions = reader.GetInt32(8),
                    ExtensionsUsed = reader.GetInt32(9),
                    ExtensionMinutes = reader.GetInt32(10),
                    EndReason = reader.GetString(11)
                });
            }
            return result;
        }
    }
}