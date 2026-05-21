using System.Text.RegularExpressions;

namespace HR_App.Services
{
    public class SqlValidator
    {
        public bool IsSafe(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return false;

            string u = sql.ToUpper();

            string[] blocked =
            {
                "INSERT","UPDATE","DELETE","DROP",
                "ALTER","EXEC","TRUNCATE","CREATE"
            };

            if (blocked.Any(x => u.Contains(x)))
                return false;

            return u.TrimStart().StartsWith("SELECT")
                || u.TrimStart().StartsWith("WITH");
        }

        public string FixSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return "";

            sql = sql.Replace("```sql", "")
                     .Replace("```", "")
                     .Replace("\r", " ")
                     .Replace("\n", " ")
                     .Trim();

            // extract SELECT
            int s = sql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
            if (s >= 0)
                sql = sql.Substring(s);

            // fix LIKE
            sql = Regex.Replace(sql,
                @"LIKE\s+N'([^%']+)'",
                "LIKE N'%$1%'",
                RegexOptions.IgnoreCase);

            // 🔥 FIX OR + AND PRECEDENCE
            sql = Regex.Replace(sql,
                @"WHERE\s+(.*?)\s+AND\s+TransDate",
                m =>
                {
                    var condition = m.Groups[1].Value;

                    if (condition.Contains(" OR "))
                        return $"WHERE ({condition}) AND TransDate";

                    return m.Value;
                },
                RegexOptions.IgnoreCase);

            return sql.Trim();
        }
    }
}