using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace GiamSatNhaMay
{
    public class DatabaseDAO
    {
        private readonly string connectionString;

        public DatabaseDAO(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // Hàm kết nối thử
        public bool TestConnection()
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi kết nối: " + ex.Message);
                return false;
            }
        }

        // Hàm kiểm tra tài khoản
        public bool CheckLogin(string username, string password)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            string query = "SELECT COUNT(*) FROM Account WHERE UserName=@user AND Pass=@pass";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@pass", password);
            int count = (int)cmd.ExecuteScalar();
            return count > 0;
        }

        public List<MachineModel> GetMachinesByGroup(string groupName)
        {
            var list = new List<MachineModel>();
            string query = @"SELECT MachineID, MachineName, GroupName, TypeName, Status, ConnectionSTT 
                             FROM Machine WHERE GroupName=@GroupName";

            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@GroupName", groupName);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MachineModel
                {
                    MachineID = (int)reader["MachineID"],
                    MachineName = reader["MachineName"].ToString(),
                    GroupName = reader["GroupName"].ToString(),
                    TypeName = reader["TypeName"].ToString(),
                    Status = reader["Status"].ToString(),
                    ConnectionSTT = reader["ConnectionSTT"].ToString()
                });
            }
            return list;
        }

        public List<MachineLogEntry> GetMachineLogsToday(int machineID)
        {
            var logs = new List<MachineLogEntry>();
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            string query = @"
                SELECT MachineID, Status, LogTime 
                FROM MachineLog
                WHERE MachineID = @id AND LogTime >= CAST(GETDATE() AS DATE)
                ORDER BY LogTime";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", machineID);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(new MachineLogEntry
                {
                    MachineID = (int)reader["MachineID"],
                    Status = reader["Status"].ToString(),
                    LogTime = (DateTime)reader["LogTime"]
                });
            }
            return logs;
        }

        public string GetMachineStatusAtStartOfDay(int machineID)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            string query = @"
                SELECT TOP 1 Status 
                FROM MachineLog 
                WHERE MachineID=@id AND LogTime < CAST(GETDATE() AS DATE)
                ORDER BY LogTime DESC";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", machineID);
            var result = cmd.ExecuteScalar();
            if (result != null)
                return result.ToString();

            query = "SELECT Status FROM Machine WHERE MachineID=@id";
            cmd.CommandText = query;
            result = cmd.ExecuteScalar();
            return result?.ToString() ?? "STOP";
        }

        public List<MachineLogEntry> GetMachineLogs(int machineID, DateTime start, DateTime end)
        {
            var logs = new List<MachineLogEntry>();
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            string query = @"
                SELECT MachineID, Status, LogTime
                FROM MachineLog
                WHERE MachineID = @id AND LogTime >= @start AND LogTime <= @end
                ORDER BY LogTime ASC";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", machineID);
            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(new MachineLogEntry
                {
                    MachineID = (int)reader["MachineID"],
                    Status = reader["Status"].ToString(),
                    LogTime = (DateTime)reader["LogTime"]
                });
            }
            return logs;
        }

        public MachineLogEntry GetLastLogBeforeOrAt(int machineID, DateTime timestamp)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            string query = @"
                SELECT TOP 1 MachineID, Status, LogTime
                FROM MachineLog
                WHERE MachineID = @id AND LogTime <= @ts
                ORDER BY LogTime DESC";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", machineID);
            cmd.Parameters.AddWithValue("@ts", timestamp);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new MachineLogEntry
                {
                    MachineID = (int)reader["MachineID"],
                    Status = reader["Status"].ToString(),
                    LogTime = (DateTime)reader["LogTime"]
                };
            }
            return null;
        }

        public string GetMachineCurrentStatusFromMachineTable(int machineID)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            string q = "SELECT Status FROM Machine WHERE MachineID = @id";
            using var cmd = new SqlCommand(q, conn);
            cmd.Parameters.AddWithValue("@id", machineID);
            var r = cmd.ExecuteScalar();
            return r?.ToString();
        }

        public List<(int MachineID, string MachineName)> GetAllMachines()
        {
            var list = new List<(int, string)>();
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            string q = "SELECT MachineID, MachineName FROM Machine ORDER BY MachineName";
            using var cmd = new SqlCommand(q, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(((int)reader["MachineID"], reader["MachineName"].ToString()));
            }
            return list;
        }

        public List<string> GetMachineTypes()
        {
            var list = new List<string>();
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            string q = "SELECT DISTINCT TypeName FROM Machine WHERE TypeName IS NOT NULL ORDER BY TypeName";
            using var cmd = new SqlCommand(q, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(reader["TypeName"].ToString());
            }
            return list;
        }

        public List<MachineOption> GetMachinesByTypeName(string typeName)
        {
            var list = new List<MachineOption>();
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            string q = "SELECT MachineID, MachineName FROM Machine WHERE TypeName = @type ORDER BY MachineName";
            using var cmd = new SqlCommand(q, conn);
            cmd.Parameters.AddWithValue("@type", typeName);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MachineOption
                {
                    Id = (int)reader["MachineID"],
                    Name = reader["MachineName"].ToString()
                });
            }
            return list;
        }
        

    }
}
