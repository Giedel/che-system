//-- Borrower_Repository.cs (store/display identities as "FirstName (Role)")

using che_system.modals.model;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Windows;
using System.Data; // add if missing

namespace che_system.repositories
{
    public class Borrower_Repository : Repository_Base
    {
        // Resolve a user identity (id_number | username | "FirstName (Role)") to "FirstName (Role)"
        public string ResolveUserDisplay(string? identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
                return "";

            // If it's already "FirstName (Role)", keep it
            int open = identity.LastIndexOf(" (", StringComparison.Ordinal);
            if (open >= 0 && identity.EndsWith(")"))
                return identity;

            using var connection = GetConnection();
            const string sql = @"
SELECT TOP 1 first_name, role 
FROM [User] 
WHERE id_number = @x OR username = @x";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@x", SqlDbType.NVarChar, 50).Value = identity;
            connection.Open();

            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                var first = rdr["first_name"] != DBNull.Value ? rdr["first_name"].ToString()! : "";
                var role = rdr["role"] != DBNull.Value ? rdr["role"].ToString()! : "";
                if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(role))
                    return $"{first} ({role})";
            }

            // Fallback: return original if user not found
            return identity;
        }

        // Get role for display or stored identity (supports "FirstName (Role)")
        private string GetUserRole(string? userIdentifier)
        {
            if (string.IsNullOrWhiteSpace(userIdentifier)) return "";

            // If already "FirstName (Role)", extract the role
            int open = userIdentifier.LastIndexOf(" (", StringComparison.Ordinal);
            if (open >= 0 && userIdentifier.EndsWith(")"))
            {
                var role = userIdentifier.Substring(open + 2, userIdentifier.Length - (open + 3)).Trim();
                return role;
            }

            using var connection = GetConnection();
            const string query = "SELECT role FROM [User] WHERE id_number = @x OR username = @x";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@x", userIdentifier);
            connection.Open();
            var result = command.ExecuteScalar();
            return result != null ? result.ToString()! : "";
        }


        // Get all borrowers
        public ObservableCollection<Borrower_Model> GetAllBorrowers()
        {
            var borrowers = new ObservableCollection<Borrower_Model>();

            using var connection = GetConnection();
            string query = @"SELECT borrower_id, name, subject_code, year_level, course, contact_number FROM Borrower";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                borrowers.Add(new Borrower_Model
                {
                    BorrowerId = Convert.ToInt32(reader["borrower_id"]),
                    Name = reader["name"].ToString() ?? string.Empty,
                    SubjectCode = reader["subject_code"] != DBNull.Value ? reader["subject_code"].ToString()! : string.Empty,
                    YearLevel = reader["year_level"] != DBNull.Value ? reader["year_level"].ToString()! : string.Empty,
                    Course = reader["course"] != DBNull.Value ? reader["course"].ToString()! : string.Empty,
                    ContactNumber = reader["contact_number"] != DBNull.Value ? reader["contact_number"].ToString()! : string.Empty
                });
            }

            return borrowers;
        }

        // Get pending slips using view
        public ObservableCollection<Slip_Model> GetPendingSlips()
        {
            var slips = new ObservableCollection<Slip_Model>();

            using var connection = GetConnection();
            string query = @"SELECT slip_id, borrower_name, subject_code, subject_title, class_schedule, instructor, date_filed, date_of_use, received_by, remarks 
                             FROM vw_Pending_Slips";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var slip = new Slip_Model
                {
                    SlipId = Convert.ToInt32(reader["slip_id"]),
                    BorrowerName = reader["borrower_name"].ToString() ?? string.Empty,
                    SubjectCode = reader["subject_code"]?.ToString() ?? string.Empty,
                    SubjectTitle = reader["subject_title"]?.ToString() ?? string.Empty,
                    ClassSchedule = reader["class_schedule"]?.ToString() ?? string.Empty,
                    Instructor = reader["instructor"]?.ToString() ?? string.Empty,
                    DateFiled = Convert.ToDateTime(reader["date_filed"]),
                    DateOfUse = Convert.ToDateTime(reader["date_of_use"]),
                    ReceivedBy = reader["received_by"]?.ToString() ?? string.Empty,
                    Remarks = reader["remarks"]?.ToString() ?? string.Empty
                };

                slip.ReceivedByRole = GetUserRole(slip.ReceivedBy);
                slip.ReleasedByRole = GetUserRole(slip.ReleasedBy);
                slip.CheckedByRole = GetUserRole(slip.CheckedBy);

                slip.Details = GetSlipDetails(slip.SlipId);
                slips.Add(slip);
            }

            return slips;
        }

        // Get active slips using view
        public ObservableCollection<Slip_Model> GetActiveSlips()
        {
            var slips = new ObservableCollection<Slip_Model>();

            using var connection = GetConnection();
            string query = @"SELECT slip_id, borrower_name, subject_code, subject_title, class_schedule, instructor, date_filed, date_of_use, received_by, released_by, remarks 
                             FROM vw_Active_Slips";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var slip = new Slip_Model
                {
                    SlipId = Convert.ToInt32(reader["slip_id"]),
                    BorrowerName = reader["borrower_name"].ToString() ?? string.Empty,
                    SubjectCode = reader["subject_code"]?.ToString() ?? string.Empty,
                    SubjectTitle = reader["subject_title"]?.ToString() ?? string.Empty,
                    ClassSchedule = reader["class_schedule"]?.ToString() ?? string.Empty,
                    Instructor = reader["instructor"]?.ToString() ?? string.Empty,
                    DateFiled = Convert.ToDateTime(reader["date_filed"]),
                    DateOfUse = Convert.ToDateTime(reader["date_of_use"]),
                    ReceivedBy = reader["received_by"]?.ToString() ?? string.Empty,
                    ReleasedBy = reader["released_by"]?.ToString() ?? string.Empty,
                    Remarks = reader["remarks"]?.ToString() ?? string.Empty
                };

                slip.ReceivedByRole = GetUserRole(slip.ReceivedBy);
                slip.ReleasedByRole = GetUserRole(slip.ReleasedBy);
                slip.CheckedByRole = GetUserRole(slip.CheckedBy);

                slip.Details = GetSlipDetails(slip.SlipId);
                slips.Add(slip);
            }

            return slips;
        }


        // Get completed slips
        public ObservableCollection<Slip_Model> GetCompletedSlips()
        {
            var slips = new ObservableCollection<Slip_Model>();

            using var connection = GetConnection();
            string query = @"SELECT bs.slip_id, b.name AS borrower_name, b.subject_code, b.subject_title, 
                                    b.class_schedule, b.instructor, bs.date_filed, bs.date_of_use, 
                                    bs.received_by, bs.released_by, bs.checked_by, bs.remarks 
                             FROM Borrower_Slip bs 
                             INNER JOIN Borrower b ON bs.borrower_id = b.borrower_id 
                             WHERE bs.checked_by IS NOT NULL";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var slip = new Slip_Model
                {
                    SlipId = Convert.ToInt32(reader["slip_id"]),
                    BorrowerName = reader["borrower_name"].ToString() ?? string.Empty,
                    SubjectCode = reader["subject_code"]?.ToString() ?? string.Empty,
                    SubjectTitle = reader["subject_title"]?.ToString() ?? string.Empty,
                    ClassSchedule = reader["class_schedule"]?.ToString() ?? string.Empty,
                    Instructor = reader["instructor"]?.ToString() ?? string.Empty,
                    DateFiled = Convert.ToDateTime(reader["date_filed"]),
                    DateOfUse = Convert.ToDateTime(reader["date_of_use"]),
                    ReceivedBy = reader["received_by"]?.ToString() ?? string.Empty,
                    ReleasedBy = reader["released_by"]?.ToString() ?? string.Empty,
                    CheckedBy = reader["checked_by"]?.ToString() ?? string.Empty,
                    Remarks = reader["remarks"]?.ToString() ?? string.Empty
                };

                slip.ReceivedByRole = GetUserRole(slip.ReceivedBy);
                slip.ReleasedByRole = GetUserRole(slip.ReleasedBy);
                slip.CheckedByRole = GetUserRole(slip.CheckedBy);

                slip.Details = GetSlipDetails(slip.SlipId);
                slips.Add(slip);
            }

            return slips;
        }

        // Get slip details
        public ObservableCollection<SlipDetail_Model> GetSlipDetails(int slipId)
        {
            var details = new ObservableCollection<SlipDetail_Model>();

            using var connection = GetConnection();
            string query = @"SELECT sd.slip_detail_id,
                                   sd.item_id,
                                   i.name AS item_name,
                                   i.type AS type,
                                   sd.quantity_borrowed,
                                   sd.quantity_released,
                                   sd.quantity_returned,
                                   sd.date_released,
                                   sd.date_returned,
                                   sd.received_by,
                                   sd.remarks
                            FROM Slip_Detail sd
                            INNER JOIN Item i ON sd.item_id = i.item_id
                            WHERE sd.slip_id = @slipId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@slipId", slipId);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                details.Add(new SlipDetail_Model
                {
                    DetailId = Convert.ToInt32(reader["slip_detail_id"]),
                    ItemId = Convert.ToInt32(reader["item_id"]),
                    ItemName = reader["item_name"].ToString() ?? string.Empty,
                    Type = reader["type"].ToString() ?? string.Empty,
                    QuantityBorrowed = Convert.ToInt32(reader["quantity_borrowed"]),
                    QuantityReleased = reader["quantity_released"] != DBNull.Value ? Convert.ToInt32(reader["quantity_released"]) : 0,
                    QuantityReturned = reader["quantity_returned"] != DBNull.Value ? Convert.ToInt32(reader["quantity_returned"]) : 0,
                    DateReleased = reader["date_released"] != DBNull.Value ? Convert.ToDateTime(reader["date_released"]) : (DateTime?)null,
                    DateReturned = reader["date_returned"] != DBNull.Value ? Convert.ToDateTime(reader["date_returned"]) : (DateTime?)null,
                    ReceivedBy = reader["received_by"] != DBNull.Value ? reader["received_by"].ToString()! : "",
                    Remarks = reader["remarks"] != DBNull.Value ? reader["remarks"].ToString()! : string.Empty
                });
            }

            return details;
        }

        // Add new borrower
        public void AddBorrower(Borrower_Model borrower)
        {
            using var connection = GetConnection();
            string query = @"INSERT INTO Borrower (name, subject_code, year_level, course, contact_number) 
                             VALUES (@name, @subject_code, @year_level, @course, @contact_number)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@name", borrower.Name);
            command.Parameters.AddWithValue("@subject_code", string.IsNullOrEmpty(borrower.SubjectCode) ? (object)DBNull.Value : borrower.SubjectCode);
            command.Parameters.AddWithValue("@year_level", string.IsNullOrEmpty(borrower.YearLevel) ? (object)DBNull.Value : borrower.YearLevel);
            command.Parameters.AddWithValue("@course", string.IsNullOrEmpty(borrower.Course) ? (object)DBNull.Value : borrower.Course);
            command.Parameters.AddWithValue("@contact_number", string.IsNullOrEmpty(borrower.ContactNumber) ? (object)DBNull.Value : borrower.ContactNumber);
            connection.Open();
            command.ExecuteNonQuery();
        }

        // Add new slip (basic)
        public int AddSlip(Slip_Model slip, string receivedByDisplay = null)
        {
            using var connection = GetConnection();
            string query = @"INSERT INTO Borrower_Slip (borrower_id, date_of_use, received_by, remarks) 
                 OUTPUT INSERTED.slip_id
                 VALUES (@borrower_id, @date_of_use, @received_by, @remarks)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@borrower_id", slip.BorrowerId);
            command.Parameters.AddWithValue("@date_of_use", slip.DateOfUse);

            var input = string.IsNullOrWhiteSpace(receivedByDisplay) ? slip.ReceivedBy : receivedByDisplay;
            var display = ResolveUserDisplay(input);

            // Debug info without blocking dialog
            System.Diagnostics.Debug.WriteLine($"[AddSlip] ReceivedBy Input: {input}");
            System.Diagnostics.Debug.WriteLine($"[AddSlip] Display Resolved: {display}");
            // REMOVE THIS BLOCKING DIALOG: System.Windows.MessageBox.Show($"[AddSlip]\nReceived By: {display}", "Debug Info");

            command.Parameters.AddWithValue("@received_by",
                string.IsNullOrWhiteSpace(display) ? (object)DBNull.Value : display);

            command.Parameters.AddWithValue("@remarks",
                string.IsNullOrEmpty(slip.Remarks) ? (object)DBNull.Value : slip.Remarks);

            connection.Open();
            return (int)command.ExecuteScalar();
        }

        // Add slip detail
        public void AddSlipDetail(int slipId, int itemId, int quantityBorrowed, string remarks = "")
        {
            using var connection = GetConnection();
            string query = @"INSERT INTO Slip_Detail (slip_id, item_id, quantity_borrowed, remarks) 
                             VALUES (@slip_id, @item_id, @quantity_borrowed, @remarks)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@slip_id", slipId);
            command.Parameters.AddWithValue("@item_id", itemId);
            command.Parameters.AddWithValue("@quantity_borrowed", quantityBorrowed);
            command.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(remarks) ? (object)DBNull.Value : remarks);
            connection.Open();
            command.ExecuteNonQuery();
        }

        // Update slip (for release) -> store "FirstName (Role)"
        public void UpdateSlipRelease(int slipId, string currentUserDisplay)
        {
            using var connection = GetConnection();
            string query = @"UPDATE Borrower_Slip 
                     SET released_by = @released_by 
                     WHERE slip_id = @slip_id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@slip_id", slipId);

            // Log for debugging
            System.Diagnostics.Debug.WriteLine($"[UpdateSlipRelease] ReleasedBy: {currentUserDisplay}");

            // Save the current user's display ("FirstName (Role)")
            command.Parameters.AddWithValue("@released_by",
                string.IsNullOrWhiteSpace(currentUserDisplay) ? (object)DBNull.Value : currentUserDisplay);

            connection.Open();
            command.ExecuteNonQuery();
        }
        public void UpdateSlipCheck(int slipId, string currentUserDisplay)
        {
            using var connection = GetConnection();
            string query = @"UPDATE Borrower_Slip 
                     SET checked_by = @checked_by 
                     WHERE slip_id = @slip_id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@slip_id", slipId);

            // Log for debugging
            System.Diagnostics.Debug.WriteLine($"[UpdateSlipCheck] CheckedBy: {currentUserDisplay}");

            // Save the current user's display ("FirstName (Role)")
            command.Parameters.AddWithValue("@checked_by",
                string.IsNullOrWhiteSpace(currentUserDisplay) ? (object)DBNull.Value : currentUserDisplay);

            connection.Open();
            command.ExecuteNonQuery();
        }

        // Update detail return
        public void UpdateDetailRelease(int detailId, int quantityReleased)
        {
            using var connection = GetConnection();

            string query = quantityReleased > 0
                ? @"UPDATE Slip_Detail 
            SET quantity_released = @quantity_released, 
                date_released = GETDATE() 
            WHERE slip_detail_id = @slip_detail_id"
                : @"UPDATE Slip_Detail 
            SET quantity_released = @quantity_released, 
                date_released = NULL 
            WHERE slip_detail_id = @slip_detail_id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@slip_detail_id", detailId);
            command.Parameters.AddWithValue("@quantity_released", quantityReleased);
            connection.Open();
            command.ExecuteNonQuery();
        }

        public void UpdateDetailReturn(int detailId, int quantityReturned)
        {
            using var connection = GetConnection();

            string query = quantityReturned > 0
                ? @"UPDATE Slip_Detail 
              SET quantity_returned = @quantity_returned, 
                  date_returned = GETDATE() 
              WHERE slip_detail_id = @slip_detail_id"
                : @"UPDATE Slip_Detail 
              SET quantity_returned = @quantity_returned, 
                  date_returned = NULL 
              WHERE slip_detail_id = @slip_detail_id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@slip_detail_id", detailId);
            command.Parameters.AddWithValue("@quantity_returned", quantityReturned);
            connection.Open();
            command.ExecuteNonQuery();
        }

        public void UpdateItemStock(int itemId, int quantityChange)
        {
            using var connection = GetConnection();
            string query = @"UPDATE Item 
                             SET quantity = quantity + @quantityChange 
                             WHERE item_id = @item_id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@item_id", itemId);
            command.Parameters.AddWithValue("@quantityChange", quantityChange);
            connection.Open();
            command.ExecuteNonQuery();
        }

        public Borrower_Model GetBorrowerBySlipId(int slipId)
        {
            using var connection = GetConnection();
            string query = @"SELECT b.borrower_id, b.name, b.subject_title, b.subject_code, b.class_schedule, b.instructor 
                             FROM Borrower b 
                             INNER JOIN Borrower_Slip bs ON b.borrower_id = bs.borrower_id 
                             WHERE bs.slip_id = @slipId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@slipId", slipId);
            connection.Open();

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Borrower_Model
                {
                    BorrowerId = Convert.ToInt32(reader["borrower_id"]),
                    Name = reader["name"].ToString() ?? string.Empty,
                    SubjectTitle = reader["subject_title"].ToString() ?? string.Empty,
                    SubjectCode = reader["subject_code"].ToString() ?? string.Empty,
                    ClassSchedule = reader["class_schedule"].ToString() ?? string.Empty,
                    Instructor = reader["instructor"].ToString() ?? string.Empty
                };
            }

            return null;
        }

        // Update detail release and stock (audit with display)
        // Update detail release and stock (store/display current user as "FirstName (Role)")
        // Update detail release and adjust stock (delta-based)
        // Replace the UpdateDetailReleaseAndStock method with this version
        public void UpdateDetailReleaseAndStock(int detailId, int itemId, int newReleased, int oldReleased, string releasedByIdentity)
        {
            int delta = newReleased - oldReleased;
            var resolvedDisplay = ResolveUserDisplay(releasedByIdentity);

            using var connection = GetConnection();
            connection.Open();
            using var tx = connection.BeginTransaction();

            try
            {
                // Debug info (no dialog)
                System.Diagnostics.Debug.WriteLine($"[UpdateDetailReleaseAndStock] ReleasedBy: {resolvedDisplay}");

                string detailSql = newReleased > 0
                    ? @"UPDATE Slip_Detail
               SET quantity_released = @qr,
                   date_released = GETDATE()
               WHERE slip_detail_id = @id"
                    : @"UPDATE Slip_Detail
               SET quantity_released = @qr,
                   date_released = NULL
               WHERE slip_detail_id = @id";

                using (var cmd = new SqlCommand(detailSql, connection, tx))
                {
                    cmd.Parameters.AddWithValue("@qr", newReleased);
                    cmd.Parameters.AddWithValue("@id", detailId);
                    cmd.ExecuteNonQuery();
                }

                // Adjust Item.quantity only when there's a change (delta)
                if (delta != 0)
                {
                    using var stockCmd = new SqlCommand(
                        "UPDATE Item SET quantity = quantity - @delta WHERE item_id = @itemId",
                        connection, tx);
                    stockCmd.Parameters.AddWithValue("@delta", delta);
                    stockCmd.Parameters.AddWithValue("@itemId", itemId);
                    stockCmd.ExecuteNonQuery();
                }

                // AUDIT LOGGING INSIDE THE TRANSACTION - matching your actual table schema
                string auditSql = @"INSERT INTO Audit_Log (user_id, action_type, description, date_time, entity_type, entity_id)
                           VALUES (@user_id, @action_type, @description, GETDATE(), @entity_type, @entity_id)";

                using (var auditCmd = new SqlCommand(auditSql, connection, tx))
                {
                    auditCmd.Parameters.AddWithValue("@user_id", resolvedDisplay);
                    auditCmd.Parameters.AddWithValue("@action_type", "Release Quantity");
                    auditCmd.Parameters.AddWithValue("@description", $"Released {newReleased} of item ID {itemId} (Detail ID {detailId})");
                    auditCmd.Parameters.AddWithValue("@entity_type", "Slip_Detail");
                    auditCmd.Parameters.AddWithValue("@entity_id", detailId.ToString());
                    auditCmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // Update detail return and stock (store/display received_by as "FirstName (Role)")
        public void UpdateDetailReturnAndStock(int detailId, int itemId, int newReturned, int oldReturned, string receivedByIdentity)
        {
            int delta = newReturned - oldReturned;
            var receivedByDisplay = ResolveUserDisplay(receivedByIdentity);

            using var connection = GetConnection();
            connection.Open();
            using var tx = connection.BeginTransaction();

            try
            {
                // Debug info without blocking dialog
                System.Diagnostics.Debug.WriteLine($"[UpdateDetailReturnAndStock] ReceivedBy Identity: {receivedByIdentity}");
                System.Diagnostics.Debug.WriteLine($"[UpdateDetailReturnAndStock] Display Resolved: {receivedByDisplay}");

                string detailSql = newReturned > 0
                    ? @"UPDATE Slip_Detail 
               SET quantity_returned=@qr, 
                   date_returned=GETDATE(), 
                   received_by=@received_by
               WHERE slip_detail_id=@id"
                    : @"UPDATE Slip_Detail 
               SET quantity_returned=@qr, 
                   date_returned=NULL, 
                   received_by=NULL
               WHERE slip_detail_id=@id";

                using (var cmd = new SqlCommand(detailSql, connection, tx))
                {
                    cmd.Parameters.AddWithValue("@qr", newReturned);
                    cmd.Parameters.AddWithValue("@id", detailId);
                    cmd.Parameters.AddWithValue("@received_by",
                        string.IsNullOrWhiteSpace(receivedByDisplay) ? (object)DBNull.Value : receivedByDisplay);
                    cmd.ExecuteNonQuery();
                }

                if (delta > 0)
                {
                    using var stockCmd = new SqlCommand(
                        "UPDATE Item SET quantity = quantity + @delta WHERE item_id = @itemId",
                        connection, tx);
                    stockCmd.Parameters.AddWithValue("@delta", delta);
                    stockCmd.Parameters.AddWithValue("@itemId", itemId);
                    stockCmd.ExecuteNonQuery();
                }

                // AUDIT LOGGING INSIDE THE TRANSACTION - matching your Audit_Log table schema
                string auditSql = @"INSERT INTO Audit_Log (user_id, action_type, description, date_time, entity_type, entity_id)
                           VALUES (@user_id, @action_type, @description, GETDATE(), @entity_type, @entity_id)";

                using (var auditCmd = new SqlCommand(auditSql, connection, tx))
                {
                    auditCmd.Parameters.AddWithValue("@user_id", receivedByDisplay);
                    auditCmd.Parameters.AddWithValue("@action_type", "Return Quantity");
                    auditCmd.Parameters.AddWithValue("@description", $"Returned {newReturned} of item ID {itemId} (Detail ID {detailId})");
                    auditCmd.Parameters.AddWithValue("@entity_type", "Slip_Detail");
                    auditCmd.Parameters.AddWithValue("@entity_id", detailId.ToString());
                    auditCmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // Delete slip by ID
        public void DeleteSlip(int slipId, bool restoreStock, string currentUser)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        if (restoreStock)
                        {
                            string restoreStockQuery = @"
                        UPDATE Item
                        SET quantity = quantity + sd.quantity_released
                        FROM Slip_Detail sd
                        WHERE sd.slip_id = @SlipId AND sd.item_id = Item.item_id";

                            using (var cmd = new SqlCommand(restoreStockQuery, conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@SlipId", slipId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (var cmd = new SqlCommand("DELETE FROM Slip_Detail WHERE slip_id = @SlipId", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@SlipId", slipId);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new SqlCommand("DELETE FROM Borrower_Slip WHERE slip_id = @SlipId", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@SlipId", slipId);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();

                        // ✅ Log audit
                        new AuditRepository().LogAction(
                            currentUser,
                            "Delete Slip",
                            $"Deleted slip ID {slipId}",
                            "Borrower_Slip",
                            slipId.ToString()
                        );
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }


        /// <summary>
        /// Returns proof image (if any) for a slip.
        /// </summary>
        public (byte[] Image, string FileName, string ContentType)? GetSlipProofImage(int slipId)
        {
            using var connection = GetConnection();
            const string sql = @"SELECT proof_image, proof_image_file_name, proof_image_content_type 
                                 FROM Borrower_Slip WHERE slip_id = @id";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", slipId);
            connection.Open();
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (rdr["proof_image"] == DBNull.Value)
                    return null;

                return (
                    (byte[])rdr["proof_image"],
                    rdr["proof_image_file_name"] != DBNull.Value ? rdr["proof_image_file_name"].ToString()! : "",
                    rdr["proof_image_content_type"] != DBNull.Value ? rdr["proof_image_content_type"].ToString()! : ""
                );
            }
            return null;
        }
    }
}
