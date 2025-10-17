//-- Dashboard_Repository.cs

using che_system.model;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

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
            Console.WriteLine($"QuickStats for range {fromYear}-{toYear}");

            if (fromYear > toYear)
            {
                (fromYear, toYear) = (toYear, fromYear);
            }

            using (var cmd = new SqlCommand($"{query} AND created_at IS NOT NULL AND YEAR(created_at) BETWEEN @fromYear AND @toYear", conn))
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

        // 🔹 System Alerts (ENHANCED + Realistic phrasing)
        // Includes:
        //  - Expired items
        //  - Expiring soon items
        //  - Low / Critical / Out of stock (using threshold)
        //  - Calibration: only for equipment that reasonably requires calibration (keyword based)
        //  - Recent logged low stock history (context only)
        // Realism adjustments:
        //   * Skip calibration messages for generic glassware/consumables (e.g., Beaker, Test Tube)
        //   * Improved phrasing for actionability
        public ObservableCollection<Alert_Model> GetSystemAlerts()
        {
            var alerts = new ObservableCollection<Alert_Model>();

            int expiringDaysAhead = 30;
            int calibrationSoonDays = 15;

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // 1. Expired items (expiry_date < today)
                    using (var cmd = new SqlCommand(@"
                        SELECT item_id, name, expiry_date, category
                        FROM Item
                        WHERE expiry_date IS NOT NULL
                          AND expiry_date < CAST(GETDATE() AS date)", conn))
                    {
                        using var r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            var exp = SafeDate(r, "expiry_date");
                            string name = SafeString(r, "name");
                            alerts.Add(new Alert_Model
                            {
                                ItemId = SafeInt(r, "item_id"),
                                Type = "Expired",
                                Message = $"Expired: {name} (expired {exp:MMM dd, yyyy}) – segregate / dispose per lab protocol.",
                                LoggedAt = exp,
                                Severity = AlertSeverity.Critical
                            });
                        }
                    }

                    // 2. Expiring soon items
                    using (var cmd = new SqlCommand(@"
                        SELECT item_id, name, expiry_date, category
                        FROM Item
                        WHERE expiry_date IS NOT NULL
                          AND expiry_date >= CAST(GETDATE() AS date)
                          AND expiry_date <= DATEADD(day, @daysAhead, CAST(GETDATE() AS date))", conn))
                    {
                        cmd.Parameters.AddWithValue("@daysAhead", expiringDaysAhead);
                        using var r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            var expDate = SafeDate(r, "expiry_date");
                            int remaining = (expDate.Date - DateTime.Today).Days;
                            string name = SafeString(r, "name");
                            alerts.Add(new Alert_Model
                            {
                                ItemId = SafeInt(r, "item_id"),
                                Type = "Expiring",
                                Message = $"Expires in {remaining} day{Plural(remaining)}: {name} (on {expDate:MMM dd, yyyy}).",
                                LoggedAt = expDate,
                                Severity = remaining <= 7 ? AlertSeverity.High : AlertSeverity.Medium
                            });
                        }
                    }

                    // 3. Stock level (Out / Critical / Low)
                    using (var cmd = new SqlCommand(@"
                        SELECT item_id, name, quantity, unit, threshold, category
                        FROM Item
                        WHERE quantity IS NOT NULL
                          AND (threshold IS NOT NULL OR quantity <= 0)", conn))
                    {
                        using var r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            int qty = SafeInt(r, "quantity");
                            int threshold = SafeInt(r, "threshold");
                            string name = SafeString(r, "name");
                            string unit = SafeString(r, "unit");

                            if (qty <= 0)
                            {
                                alerts.Add(new Alert_Model
                                {
                                    ItemId = SafeInt(r, "item_id"),
                                    Type = "Out of Stock",
                                    Message = $"Out of stock: {name} – restock required for upcoming sessions.",
                                    LoggedAt = DateTime.Now,
                                    Severity = AlertSeverity.Critical
                                });
                            }
                            else if (threshold > 0)
                            {
                                int criticalThreshold = Math.Max(1, threshold / 2);
                                if (qty <= criticalThreshold)
                                {
                                    alerts.Add(new Alert_Model
                                    {
                                        ItemId = SafeInt(r, "item_id"),
                                        Type = "Critical Stock",
                                        Message = $"Critical stock: {name} – {qty} {unit} remaining (threshold {threshold}).",
                                        LoggedAt = DateTime.Now,
                                        Severity = AlertSeverity.High
                                    });
                                }
                                else if (qty <= threshold)
                                {
                                    alerts.Add(new Alert_Model
                                    {
                                        ItemId = SafeInt(r, "item_id"),
                                        Type = "Low Stock",
                                        Message = $"Low stock: {name} – {qty} {unit} remaining (threshold {threshold}).",
                                        LoggedAt = DateTime.Now,
                                        Severity = AlertSeverity.Medium
                                    });
                                }
                            }
                        }
                    }

                    // 4. Recent low stock log (context, not escalation)
                    using (var cmd = new SqlCommand(@"
                        SELECT l.item_id, i.name, i.unit, l.quantity_at_that_time, l.logged_at
                        FROM LowStock_Log l
                        INNER JOIN Item i ON i.item_id = l.item_id
                        WHERE l.logged_at >= DATEADD(day, -7, GETDATE())
                        ORDER BY l.logged_at DESC", conn))
                    {
                        using var r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            alerts.Add(new Alert_Model
                            {
                                ItemId = SafeInt(r, "item_id"),
                                Type = "Low Stock (History)",
                                Message = $"Previously flagged: {SafeString(r, "name")} was at {SafeInt(r, "quantity_at_that_time")} {SafeString(r, "unit")} on {SafeDate(r, "logged_at"):MMM dd}.",
                                LoggedAt = SafeDate(r, "logged_at"),
                                Severity = AlertSeverity.Info
                            });
                        }
                    }

                    // 5. Calibration (only meaningful equipment)
                    using (var cmd = new SqlCommand(@"
                        SELECT item_id, name, calibration_date, category, type
                        FROM Item
                        WHERE (category = 'Apparatus' OR type = 'non-consumable')", conn))
                    {
                        using var r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            string name = SafeString(r, "name");
                            if (!RequiresCalibration(name))
                                continue; // Skip non-calibrated apparatus like glassware

                            DateTime? scheduled = SafeNullableDate(r, "calibration_date");
                            int id = SafeInt(r, "item_id");

                            if (scheduled == null)
                            {
                                alerts.Add(new Alert_Model
                                {
                                    ItemId = id,
                                    Type = "Calibration Missing",
                                    Message = $"Set calibration schedule: {name} has no recorded next calibration.",
                                    LoggedAt = DateTime.Now,
                                    Severity = AlertSeverity.Medium
                                });
                                continue;
                            }

                            if (scheduled.Value.Date < DateTime.Today)
                            {
                                int overdue = (DateTime.Today - scheduled.Value.Date).Days;
                                alerts.Add(new Alert_Model
                                {
                                    ItemId = id,
                                    Type = "Calibration Overdue",
                                    Message = $"Calibration overdue: {name} – {overdue} day{Plural(overdue)} past due (was {scheduled:MMM dd, yyyy}).",
                                    LoggedAt = scheduled.Value,
                                    Severity = AlertSeverity.High
                                });
                            }
                            else
                            {
                                int daysLeft = (scheduled.Value.Date - DateTime.Today).Days;
                                if (daysLeft <= calibrationSoonDays)
                                {
                                    alerts.Add(new Alert_Model
                                    {
                                        ItemId = id,
                                        Type = "Calibration Due Soon",
                                        Message = $"Calibration due in {daysLeft} day{Plural(daysLeft)}: {name} ({scheduled:MMM dd, yyyy}).",
                                        LoggedAt = scheduled.Value,
                                        Severity = daysLeft <= 7 ? AlertSeverity.Medium : AlertSeverity.Low
                                    });
                                }
                            }
                        }
                    }
                }

                // Final ordering: Severity first, then most urgent (earliest LoggedAt), then alphabetic
                var ordered = alerts
                    .OrderBy(a => a.Severity)
                    .ThenBy(a => a.LoggedAt)
                    .ThenBy(a => a.Message)
                    .ToList();

                alerts = new ObservableCollection<Alert_Model>(ordered);
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert_Model
                {
                    Type = "System",
                    Message = $"Failed to load alerts: {ex.Message}",
                    LoggedAt = DateTime.Now,
                    Severity = AlertSeverity.System
                });
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
            public int? ItemId { get; set; }          // Optional reference
            public string Type { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public DateTime LoggedAt { get; set; }
            public AlertSeverity Severity { get; set; } = AlertSeverity.Info; // Added for ordering / styling (optional)
        }

        public enum AlertSeverity
        {
            Critical = 0,
            High = 1,
            Medium = 2,
            Low = 3,
            Info = 4,
            System = 5
        }

        public class Activity_Model
        {
            public string UserId { get; set; } = string.Empty;
            public string ActionType { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime DateTime { get; set; }
        }

        // 🔹 Helper safe-read methods
        private static int SafeInt(SqlDataReader r, string col) =>
            r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);

        private static string SafeString(SqlDataReader r, string col) =>
            r[col] == DBNull.Value ? string.Empty : r[col].ToString()!;

        private static DateTime SafeDate(SqlDataReader r, string col) =>
            r[col] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(r[col]);

        private static DateTime? SafeNullableDate(SqlDataReader r, string col) =>
            r[col] == DBNull.Value ? null : Convert.ToDateTime(r[col]);

        // 🔹 Domain helpers
        private static string Plural(int v) => v == 1 ? "" : "s";

        // Basic heuristic of equipment that normally requires calibration
        private static readonly string[] CalibrationKeywords = new[]
        {
            "balance","scale","meter","ph","pH","pipette","burette","thermometer",
            "spectro","spectrophotometer","centrifuge","incubator","chromatograph",
            "analyzer","sensor","conductivity","titrator"
        };

        private static bool RequiresCalibration(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string lower = name.ToLowerInvariant();
            return CalibrationKeywords.Any(k => lower.Contains(k));
        }
    }
}
