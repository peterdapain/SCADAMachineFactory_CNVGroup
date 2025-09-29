using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace GiamSatNhaMay
{
    public class InfoWindowDAO
    {
        private readonly string _connectionString;

        public InfoWindowDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Lấy danh sách máy
        public List<MachineModel> GetAllMachines()
        {
            var list = new List<MachineModel>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            string query = "SELECT MachineID, MachineName, GroupName, TypeName, Status, ConnectionSTT FROM Machine ORDER BY MachineName";
            using var cmd = new SqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MachineModel
                {
                    MachineID = reader.GetInt32(0),
                    MachineName = reader.GetString(1),
                    GroupName = reader["GroupName"]?.ToString(),
                    TypeName = reader["TypeName"]?.ToString(),
                    Status = reader["Status"]?.ToString(),
                    ConnectionSTT = reader["ConnectionSTT"]?.ToString()
                });
            }
            return list;
        }


        // Lấy thông tin máy (Info + Maintenance + MachineError)
        public MachineInfo GetMachineInfo(int machineID)
        {
            var info = new MachineInfo();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Lấy Info cơ bản
            string queryInfo = @"SELECT Manufacturer, StartDate, ContactPerson FROM Info WHERE MachineID=@id";
            using (var cmd = new SqlCommand(queryInfo, conn))
            {
                cmd.Parameters.AddWithValue("@id", machineID);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    info.Manufacturer = reader["Manufacturer"]?.ToString();
                    info.StartDate = reader["StartDate"] != DBNull.Value ? (DateTime?)reader["StartDate"] : null;
                    info.ContactPerson = reader["ContactPerson"]?.ToString();
                }
            }

            // Lấy lần bảo trì gần nhất
            string queryMaintenance = @"SELECT TOP 1 MaintenanceDate 
                                        FROM Maintenance 
                                        WHERE MachineID=@id 
                                        ORDER BY MaintenanceDate DESC";

            using (var cmd = new SqlCommand(queryMaintenance, conn))
            {
                cmd.Parameters.AddWithValue("@id", machineID);
                var result = cmd.ExecuteScalar();
                info.LastMaintenance = result != null ? (DateTime?)result : null;
            }
            // 
            // Lấy các mã lỗi gần đây (ví dụ 5 lỗi gần nhất)
            string queryError = @"SELECT TOP 5 ErrorCode 
                                  FROM MachineError 
                                  WHERE MachineID=@id 
                                  ORDER BY ErrorTime DESC";
            using (var cmd = new SqlCommand(queryError, conn))
            {
                cmd.Parameters.AddWithValue("@id", machineID);
                using var reader = cmd.ExecuteReader();
                var errors = new List<string>();
                while (reader.Read())
                {
                    errors.Add(reader["ErrorCode"]?.ToString());
                }
                info.ErrorCodes = string.Join(", ", errors);
            }

            return info;
        }

        // Cập nhật thông tin cơ bản máy
        public void UpdateMachineInfo(int machineID, MachineInfo info)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            // Nếu đã có row thì update, nếu chưa có thì insert
            string query = @"
        IF EXISTS (SELECT 1 FROM Info WHERE MachineID = @id)
            UPDATE Info
            SET Manufacturer = @manu,
                StartDate = @start,
                ContactPerson = @contact
            WHERE MachineID = @id
        ELSE
            INSERT INTO Info (MachineID, Manufacturer, StartDate, ContactPerson)
            VALUES (@id, @manu, @start, @contact)";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", machineID);
            cmd.Parameters.AddWithValue("@manu", info.Manufacturer ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@start", info.StartDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@contact", info.ContactPerson ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }
    }
}
