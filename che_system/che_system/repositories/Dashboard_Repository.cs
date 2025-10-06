//-- Dashboard_Repository.cs

using che_system.model;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;

namespace che_system.repositories
{
    public class Dashboard_Repository : Repository_Base
    {
        // 🔹 RANGE-BASED QuickStats
        public ObservableCollection<Quick_Stat_Model> GetQuickStatsRange(int fromYear, int toYear)
        {
            var stats = new ObservableCollection<Quick_Stat_Model>();

            using (var conn = GetConnection())
            {
                conn.Open();

                stats.Add(new Quick_Stat_Model
                {
                    Title = "Total Chemicals",
                    Value = ExecuteCountRange(conn, "SELECT COUNT(*) FROM Item WHERE category = 'Chemical'", fromYear, toYear),
                    Icon = IconChar.Flask
                });

                stats.Add(new Quick_Stat_Model
                {
                    Title = "Apparatus Count",
                    Value = ExecuteCountRange(conn, "SELECT COUNT(*) FROM Item WHERE category = 'Apparatus'", fromYear, toYear),
                    Icon = IconChar.Cogs
                });

                stats.Add(new Quick_Stat_Model
                {
                    Title = "Total Supplies",
                    Value = ExecuteCountRange(conn, "SELECT COUNT(*) FROM Item WHERE category = 'Supplies'", fromYear, toYear),
                    Icon = IconChar.Box
                });

                stats.Add(new Quick_Stat_Model
                {
                    Title = "Miscellaneous",
                    Value = ExecuteCountRange(conn, "SELECT COUNT(*) FROM Item WHERE category = 'Miscellaneous'", fromYear, toYear),
                    Icon = IconChar.Cubes
                });
            }

            return stats;
        }

        private int ExecuteCountRange(SqlConnection conn, string query, int fromYear, int toYear)
        {
            using (var cmd = new SqlCommand($"{query} AND YEAR(created_at) BETWEEN @fromYear AND @toYear", conn))
            {
                cmd.Parameters.AddWithValue("@fromYear", fromYear);
                cmd.Parameters.AddWithValue("@toYear", toYear);

                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        // 🔹 RANGE-BASED ItemUsage
        public ObservableCollection<che_system.modals.model.Item_Usage_Model> GetItemUsageRange(int fromYear, int toYear)
        {
            var usage = new ObservableCollection<che_system.modals.model.Item_Usage_Model>();

            using (var conn = GetConnection())
            {
                conn.Open();

                // ✅ Filtered by created_at year between range
                using (var cmd = new SqlCommand(
                    "SELECT * FROM vw_Item_Usage WHERE YEAR(created_at) BETWEEN @fromYear AND @toYear", conn))
                {
                    cmd.Parameters.AddWithValue("@fromYear", fromYear);
                    cmd.Parameters.AddWithValue("@toYear", toYear);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usage.Add(new che_system.modals.model.Item_Usage_Model
                            {
                                ItemId = reader["item_id"] != DBNull.Value ? Convert.ToInt32(reader["item_id"]) : 0,
                                ItemName = reader["item_name"].ToString() ?? string.Empty,
                                Category = reader["category"] != DBNull.Value ? reader["category"].ToString()! : string.Empty,
                                TotalBorrowed = reader["total_borrowed"] != DBNull.Value ? Convert.ToInt32(reader["total_borrowed"]) : 0,
                                TotalReturned = reader["total_returned"] != DBNull.Value ? Convert.ToInt32(reader["total_returned"]) : 0,
                                UsageCount = reader["usage_count"] != DBNull.Value ? Convert.ToInt32(reader["usage_count"]) : 0
                            });
                        }
                    }
                }
            }

            return usage;
        }

        // 🔹 Existing method for year list (unchanged)
        public ObservableCollection<int> GetAvailableYears()
        {
            var years = new ObservableCollection<int>();

            using (var conn = GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT DISTINCT YEAR(created_at) AS yr FROM Item
                    UNION
                    SELECT DISTINCT YEAR(created_at) AS yr FROM Borrower_Slip
                    UNION
                    SELECT DISTINCT YEAR(created_at) AS yr FROM Slip_Detail
                    ORDER BY yr DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            years.Add(Convert.ToInt32(reader["yr"]));
                        }
                    }
                }
            }

            return years;
        }

        // 🔹 System Alerts (Expiring + Low Stock)
        public ObservableCollection<Alert_Model> GetSystemAlerts()
        {
            var alerts = new ObservableCollection<Alert_Model>();

            using (var conn = GetConnection())
            {
                conn.Open();

                // Expiring items within 30 days
                using (var cmd = new SqlCommand("sp_GetExpiringItems", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@daysAhead", 30);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            alerts.Add(new Alert_Model
                            {
                                Type = "Expiring",
                                Message = $"{reader["name"]} will expire on {Convert.ToDateTime(reader["expiry_date"]).ToShortDateString()}",
                                LoggedAt = Convert.ToDateTime(reader["expiry_date"])
                            });
                        }
                    }
                }

                // Low Stock Items (Current Year)
                using (var cmd = new SqlCommand("sp_GetLowStockItems", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@year", DateTime.Now.Year);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            alerts.Add(new Alert_Model
                            {
                                Type = "Low Stock",
                                Message = $"{reader["name"]} is low on stock ({reader["quantity"]} {reader["unit"]})",
                                LoggedAt = Convert.ToDateTime(reader["logged_at"])
                            });
                        }
                    }
                }
            }

            return alerts;
        }

        // 🔹 Recent Activity (from Audit_Log)
        public ObservableCollection<Activity_Model> GetRecentActivities(int limit = 10)
        {
            var activities = new ObservableCollection<Activity_Model>();

            using (var conn = GetConnection())
            {
                conn.Open();

                string query = @"SELECT TOP (@limit) user_id, action_type, description, date_time
                         FROM Audit_Log
                         ORDER BY date_time DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@limit", limit);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activities.Add(new Activity_Model
                            {
                                UserId = reader["user_id"].ToString(),
                                ActionType = reader["action_type"].ToString(),
                                Description = reader["description"].ToString(),
                                DateTime = Convert.ToDateTime(reader["date_time"])
                            });
                        }
                    }
                }
            }

            return activities;
        }

        // 🔹 Models
        public class Alert_Model
        {
            public string Type { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public DateTime LoggedAt { get; set; }
        }

        public class Activity_Model
        {
            public string UserId { get; set; } = string.Empty;
            public string ActionType { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime DateTime { get; set; }
        }

    }
}
