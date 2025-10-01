//-- Dashboard_Repository.cs

using che_system.model;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;

namespace che_system.repositories
{
    public class Dashboard_Repository : Repository_Base
    {
        // Get QuickStats for "All Time"
        public ObservableCollection<Quick_Stat_Model> GetQuickStats_AllTime()
        {
            var stats = new ObservableCollection<Quick_Stat_Model>();

            using (var conn = GetConnection())
            {
                conn.Open();

                // 1. Total Chemicals
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Item WHERE category = 'Chemical'", conn))
                {
                    stats.Add(new Quick_Stat_Model
                    {
                        Title = "Total Chemicals",
                        Value = (int)cmd.ExecuteScalar(),
                        Icon = IconChar.Flask
                    });
                }

                // 2. Apparatus Count
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Item WHERE category = 'Apparatus'", conn))
                {
                    stats.Add(new Quick_Stat_Model
                    {
                        Title = "Apparatus Count",
                        Value = (int)cmd.ExecuteScalar(),
                        Icon = IconChar.Cogs
                    });
                }

                // 3. Total Supplies
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Item WHERE category = 'Supplies'", conn))
                {
                    stats.Add(new Quick_Stat_Model
                    {
                        Title = "Total Supplies",
                        Value = (int)cmd.ExecuteScalar(),
                        Icon = IconChar.Box
                    });
                }

                // 4. Miscellaneous
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Item WHERE category = 'Miscellaneous'", conn))
                {
                    stats.Add(new Quick_Stat_Model
                    {
                        Title = "Miscellaneous",
                        Value = (int)cmd.ExecuteScalar(),
                        Icon = IconChar.Cubes
                    });
                }
            }

            return stats;
        }

        public ObservableCollection<Quick_Stat_Model> GetQuickStats(int? year = null)
        {
            var stats = new ObservableCollection<Quick_Stat_Model>();

            using (var conn = GetConnection())
            {
                conn.Open();

                stats.Add(new Quick_Stat_Model
                {
                    Title = "Total Chemicals",
                    Value = ExecuteCount(conn, "SELECT COUNT(*) FROM Item WHERE category = 'Chemical'", year),
                    Icon = IconChar.Flask
                });

                stats.Add(new Quick_Stat_Model
                {
                    Title = "Apparatus Count",
                    Value = ExecuteCount(conn, "SELECT COUNT(*) FROM Item WHERE category = 'Apparatus'", year),
                    Icon = IconChar.Cogs
                });

                stats.Add(new Quick_Stat_Model
                {
                    Title = "Total Supplies",
                    Value = ExecuteCount(conn, "SELECT COUNT(*) FROM Item WHERE category = 'Supplies'", year),
                    Icon = IconChar.Box
                });

                stats.Add(new Quick_Stat_Model
                {
                    Title = "Miscellaneous",
                    Value = ExecuteCount(conn, "SELECT COUNT(*) FROM Item WHERE category = 'Miscellaneous'", year),
                    Icon = IconChar.Cubes
                });
            }

            return stats;
        }

        private int ExecuteCount(SqlConnection conn, string query, int? year)
        {
            // if year is null → All Time
            if (!year.HasValue)
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    return (int)cmd.ExecuteScalar();
                }
            }

            // cumulative up to selected year
            using (var cmd = new SqlCommand($"{query} AND YEAR(created_at) <= @year", conn))
            {
                cmd.Parameters.AddWithValue("@year", year.Value);
                return (int)cmd.ExecuteScalar();
            }
        }


        public int GetStatValueForYear(string statTitle, int year)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                SqlCommand cmd;

                switch (statTitle)
                {
                    case "Total Chemicals":
                        cmd = new SqlCommand("SELECT COUNT(*) FROM Item WHERE category = 'Chemical' AND YEAR(created_at) = @year", conn);
                        break;

                    case "Apparatus Count":
                        cmd = new SqlCommand("SELECT COUNT(*) FROM Item WHERE category = 'Apparatus' AND YEAR(created_at) = @year", conn);
                        break;

                    case "Total Supplies":
                        cmd = new SqlCommand("SELECT COUNT(*) FROM Item WHERE category = 'Supplies' AND YEAR(created_at) = @year", conn);
                        break;

                    case "Miscellaneous":
                        cmd = new SqlCommand("SELECT COUNT(*) FROM Item WHERE category = 'Miscellaneous' AND YEAR(created_at) = @year", conn);
                        break;

                    default:
                        return 0;
                }

                cmd.Parameters.AddWithValue("@year", year);
                return (int)cmd.ExecuteScalar();
            }
        }


        // 🔹 Original: no filter
        public ObservableCollection<che_system.modals.model.Item_Usage_Model> GetItemUsage()
        {
            var usage = new ObservableCollection<che_system.modals.model.Item_Usage_Model>();

            using (var conn = GetConnection())
            {
                conn.Open();

                using (var cmd = new SqlCommand("SELECT * FROM vw_Item_Usage", conn))
                {
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

        // 🔹 NEW: Get ItemUsage for a specific year (Slip_Detail.created_at or Borrower_Slip.created_at)
        public ObservableCollection<che_system.modals.model.Item_Usage_Model> GetItemUsage(int year)
        {
            var usage = new ObservableCollection<che_system.modals.model.Item_Usage_Model>();

            using (var conn = GetConnection())
            {
                conn.Open();

                // ✅ vw_Item_Usage must include created_at from Borrower_Slip or Slip_Detail
                using (var cmd = new SqlCommand("SELECT * FROM vw_Item_Usage WHERE YEAR(created_at) = @year", conn))
                {
                    cmd.Parameters.AddWithValue("@year", year);

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
                                TotalReturned = reader["total_returned"] != DBNull.Value ? Convert.ToInt32(reader["total_returned"]) : 0
                            });
                        }
                    }
                }
            }

            return usage;
        }

        // 🔹 Borrower Activity (unchanged)
        public ObservableCollection<che_system.modals.model.Borrower_Activity_Model> GetBorrowerActivitySummary()
        {
            var activities = new ObservableCollection<che_system.modals.model.Borrower_Activity_Model>();

            using (var conn = GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT b.borrower_id, b.name, b.subject_code, 
                           COUNT(bs.slip_id) AS total_slips,
                           COUNT(CASE WHEN bs.checked_by IS NOT NULL THEN 1 END) AS completed_slips,
                           COUNT(CASE WHEN bs.checked_by IS NULL AND bs.released_by IS NOT NULL THEN 1 END) AS active_slips,
                           COUNT(CASE WHEN bs.released_by IS NULL THEN 1 END) AS pending_slips
                    FROM Borrower b 
                    LEFT JOIN Borrower_Slip bs ON b.borrower_id = bs.borrower_id 
                    GROUP BY b.borrower_id, b.name, b.subject_code
                    ORDER BY total_slips DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activities.Add(new che_system.modals.model.Borrower_Activity_Model
                            {
                                BorrowerId = Convert.ToInt32(reader["borrower_id"]),
                                Name = reader["name"].ToString() ?? string.Empty,
                                SubjectCode = reader["subject_code"] != DBNull.Value ? reader["subject_code"].ToString()! : string.Empty,
                                TotalSlips = Convert.ToInt32(reader["total_slips"]),
                                CompletedSlips = Convert.ToInt32(reader["completed_slips"]),
                                ActiveSlips = Convert.ToInt32(reader["active_slips"]),
                                PendingSlips = Convert.ToInt32(reader["pending_slips"])
                            });
                        }
                    }
                }
            }

            return activities;
        }

        // 🔹 NEW: Get available years
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
    }
}
