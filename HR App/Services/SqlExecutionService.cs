using Microsoft.Data.SqlClient;
using System.Data;

namespace HR_App.Services
{
    public class SqlExecutionService
    {
        private readonly string _connStr;

        public SqlExecutionService(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection");
        }

        // ✅ للـ KPI / رقم واحد
        public string ExecuteScalar(string sql)
        {
            using var con = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, con);

            cmd.CommandTimeout = 120;

            con.Open();

            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return "لا توجد بيانات";

            return result.ToString();
        }

        // ✅ للـ Tables / Multi-row
        public List<Dictionary<string, object>> ExecuteQuery(string sql)
        {
            using var con = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, con);

            cmd.CommandTimeout = 120;

            con.Open();

            using var reader = cmd.ExecuteReader();

            var result = new List<Dictionary<string, object>>();

            while (reader.Read())
            {
                var row = new Dictionary<string, object>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader[i];
                }

                result.Add(row);
            }

            return result;
        }
    }
}