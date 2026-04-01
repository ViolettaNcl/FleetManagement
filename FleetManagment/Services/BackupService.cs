using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;

namespace Zachet.Services
{
    public class BackupService
    {
        public bool ExportSelectedTablesToSql(string outputFolder, bool users, bool drivers, bool vehicles, bool routes)
        {
            try
            {
                var tables = new List<string>();
                if (users) tables.Add("Users");
                if (drivers) tables.Add("Drivers");
                if (vehicles) tables.Add("Vehicles");
                if (routes) tables.Add("Routes");

                if (tables.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы одну таблицу", "Предупреждение");
                    return false;
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string filePath = Path.Combine(outputFolder, $"Fleet_Backup_{timestamp}.sql");

                var sb = new StringBuilder();
                sb.AppendLine("-- Бэкап выбранных таблиц: " + string.Join(", ", tables));
                sb.AppendLine("-- Дата: " + DateTime.Now);
                sb.AppendLine();

                string connStr = "Server=DESKTOP-ISN4AVJ;Database=FleetManagementDB;Integrated Security=True;TrustServerCertificate=True;";

                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    foreach (var table in tables)
                    {
                        sb.AppendLine($"-- Таблица: {table}");
                        sb.AppendLine();

                        using (var cmd = new SqlCommand($"SELECT * FROM [{table}]", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                var cols = new List<string>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                    cols.Add($"[{reader.GetName(i)}]");

                                string colsLine = string.Join(", ", cols);

                                while (reader.Read())
                                {
                                    var vals = new List<string>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        var v = reader[i];
                                        if (v == DBNull.Value) vals.Add("NULL");
                                        else if (v is string s) vals.Add($"'{s.Replace("'", "''")}'");
                                        else if (v is DateTime dt) vals.Add($"'{dt:yyyy-MM-dd HH:mm:ss.fff}'");
                                        else vals.Add(v.ToString());
                                    }
                                    sb.AppendLine($"INSERT INTO [{table}] ({colsLine}) VALUES ({string.Join(", ", vals)});");
                                }
                            }
                        }
                        sb.AppendLine("GO");
                        sb.AppendLine();
                    }
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"Бэкап готов:\n{filePath}\n\n(откройте в SSMS и выполните для восстановления)", "Готово");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка:\n" + ex.Message, "Ошибка");
                return false;
            }
        }
    }
}