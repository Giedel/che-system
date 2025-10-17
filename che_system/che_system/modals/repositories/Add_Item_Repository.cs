using che_system.modals.model;
using che_system.repositories;
using Microsoft.Data.SqlClient;
using System;

namespace che_system.modals.repositories
{
    public class Add_Item_Repository : Repository_Base
    {
        /// <summary>
        /// Attempts to insert a new item. If a duplicate (per matching rule) exists, it only
        /// increases the quantity and (optionally) updates nullable fields if new values provided.
        /// Also sets Item.received_by to the current user in format 'FirstName (Role)'.
        /// Returns result info.
        /// </summary>
        public SaveItemResult AddOrMergeItem(Add_Item_Model item, string currentUser)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            // Build 'received_by' value: FirstName (Role)
            var receivedBy = BuildReceivedByTag(currentUser);

            try
            {
                using var connection = GetConnection();
                connection.Open();

                using var tx = connection.BeginTransaction();

                try
                {
                    // Determine duplicate criteria:
                    // Chemicals: Name + Alt_Name(ChemicalFormula) + Unit (case-insensitive) + same expiry (null=same)
                    // Others:   Name + Category + Unit + Type (case-insensitive)
                    string duplicateQuery;
                    if (string.Equals(item.Category, "Chemical", StringComparison.OrdinalIgnoreCase))
                    {
                        duplicateQuery = @"
                        SELECT TOP 1 item_id, quantity
                        FROM Item
                        WHERE LOWER(name) = LOWER(@Name)
                          AND LOWER(ISNULL(alt_name,'')) = LOWER(ISNULL(@Alt_Name,''))
                          AND LOWER(unit) = LOWER(@Unit)
                          AND (
                                (expiry_date IS NULL AND @Expiry_Date IS NULL)
                                OR (expiry_date = @Expiry_Date)
                              )";
                    }
                    else
                    {
                        duplicateQuery = @"
                        SELECT TOP 1 item_id, quantity
                        FROM Item
                        WHERE LOWER(name) = LOWER(@Name)
                          AND LOWER(category) = LOWER(@Category)
                          AND LOWER(unit) = LOWER(@Unit)
                          AND LOWER(ISNULL(type,'')) = LOWER(ISNULL(@Type,''))";
                    }

                    int? existingId = null;
                    int existingQty = 0;

                    using (var checkCmd = new SqlCommand(duplicateQuery, connection, tx))
                    {
                        checkCmd.Parameters.AddWithValue("@Name", item.ItemName);
                        checkCmd.Parameters.AddWithValue("@Alt_Name", (object?)item.ChemicalFormula ?? DBNull.Value);
                        checkCmd.Parameters.AddWithValue("@Category", item.Category);
                        checkCmd.Parameters.AddWithValue("@Unit", item.Unit);
                        checkCmd.Parameters.AddWithValue("@Type", (object?)item.Type ?? DBNull.Value);
                        checkCmd.Parameters.AddWithValue("@Expiry_Date", item.ExpiryDate.HasValue ? item.ExpiryDate : (object)DBNull.Value);

                        using var r = checkCmd.ExecuteReader();
                        if (r.Read())
                        {
                            existingId = Convert.ToInt32(r["item_id"]);
                            existingQty = Convert.ToInt32(r["quantity"]);
                        }
                    }

                    if (existingId.HasValue)
                    {
                        // Merge: increase quantity & update optional fields if newly provided.
                        using var updateCmd = new SqlCommand(@"
                        UPDATE Item
                        SET quantity = quantity + @AddQty,
                            location = CASE WHEN @Location IS NULL OR LTRIM(RTRIM(@Location)) = '' THEN location ELSE @Location END,
                            expiry_date = CASE WHEN @Expiry_Date IS NULL THEN expiry_date ELSE @Expiry_Date END,
                            calibration_date = CASE WHEN @Calibration IS NULL THEN calibration_date ELSE @Calibration END,
                            threshold = CASE WHEN @Threshold IS NULL THEN threshold ELSE @Threshold END,
                            received_by = @ReceivedBy,
                            modified_at = GETDATE()
                        WHERE item_id = @Id;

                        SELECT quantity FROM Item WHERE item_id = @Id;
                    ", connection, tx);

                        updateCmd.Parameters.AddWithValue("@AddQty", item.Quantity);
                        updateCmd.Parameters.AddWithValue("@Location", string.IsNullOrWhiteSpace(item.Location) ? DBNull.Value : item.Location);
                        updateCmd.Parameters.AddWithValue("@Expiry_Date", item.ExpiryDate.HasValue ? item.ExpiryDate : (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@Calibration", item.CalibrationDate.HasValue ? item.CalibrationDate : (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@Threshold", item.Threshold > 0 ? item.Threshold : (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@ReceivedBy", string.IsNullOrWhiteSpace(receivedBy) ? (object)DBNull.Value : receivedBy);
                        updateCmd.Parameters.AddWithValue("@Id", existingId.Value);

                        var newQty = Convert.ToInt32(updateCmd.ExecuteScalar());

                        tx.Commit();

                        SafeLog(currentUser,
                            "Merge Item",
                            $"Merged item '{item.ItemName}' (ID: {existingId.Value}). Quantity updated from {existingQty} to {newQty}. Received by: {receivedBy}.",
                            "Item",
                            existingId.Value.ToString());

                        return new SaveItemResult
                        {
                            ItemId = existingId.Value,
                            IsNew = false,
                            UpdatedExisting = true,
                            NewQuantity = newQty,
                            Message = $"Existing item updated. Quantity: {existingQty} -> {newQty}."
                        };
                    }
                    else
                    {
                        // Insert new
                        using var insertCmd = new SqlCommand(@"
                        INSERT INTO Item
                            (name, alt_name, quantity, unit, category, location, expiry_date, type, threshold, calibration_date, status, received_by, created_at)
                        VALUES
                            (@Name, @Alt_Name, @Quantity, @Unit, @Category, @Location, @Expiry_Date, @Type, @Threshold, @Calibration, @Status, @ReceivedBy, GETDATE());
                        SELECT SCOPE_IDENTITY();
                    ", connection, tx);

                        insertCmd.Parameters.AddWithValue("@Name", item.ItemName);
                        insertCmd.Parameters.AddWithValue("@Alt_Name", (object?)item.ChemicalFormula ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                        insertCmd.Parameters.AddWithValue("@Unit", item.Unit);
                        insertCmd.Parameters.AddWithValue("@Category", item.Category);
                        insertCmd.Parameters.AddWithValue("@Location", string.IsNullOrWhiteSpace(item.Location) ? DBNull.Value : item.Location);
                        insertCmd.Parameters.AddWithValue("@Expiry_Date", item.ExpiryDate.HasValue ? item.ExpiryDate : (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Type", string.IsNullOrWhiteSpace(item.Type) ? DBNull.Value : item.Type);
                        insertCmd.Parameters.AddWithValue("@Threshold", item.Threshold > 0 ? item.Threshold : (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Calibration", item.CalibrationDate.HasValue ? item.CalibrationDate : (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(item.Status) ? "Available" : item.Status);
                        insertCmd.Parameters.AddWithValue("@ReceivedBy", string.IsNullOrWhiteSpace(receivedBy) ? (object)DBNull.Value : receivedBy);

                        var idObj = insertCmd.ExecuteScalar();
                        var newId = Convert.ToInt32(idObj);

                        tx.Commit();

                        SafeLog(currentUser,
                            "Add Item",
                            $"Added new item '{item.ItemName}' (Category: {item.Category}, Quantity: {item.Quantity}). Received by: {receivedBy}.",
                            "Item",
                            newId.ToString());

                        return new SaveItemResult
                        {
                            ItemId = newId,
                            IsNew = true,
                            UpdatedExisting = false,
                            NewQuantity = item.Quantity,
                            Message = "New item created successfully."
                        };
                    }
                }
                catch (SqlException sqlEx)
                {
                    TryRollback(tx);
                    SafeLog(currentUser, "Add/Merge Item Failed (SQL)", $"{GetFriendlySqlMessage(sqlEx)} | Details: {sqlEx.Message}", "Item");
                    return new SaveItemResult
                    {
                        Error = true,
                        Message = GetFriendlySqlMessage(sqlEx)
                    };
                }
                catch (InvalidOperationException invEx)
                {
                    TryRollback(tx);
                    SafeLog(currentUser, "Add/Merge Item Failed (InvalidOperation)", invEx.Message, "Item");
                    return new SaveItemResult
                    {
                        Error = true,
                        Message = $"Operation failed: {invEx.Message}"
                    };
                }
                catch (Exception ex)
                {
                    TryRollback(tx);
                    SafeLog(currentUser, "Add/Merge Item Failed", ex.Message, "Item");
                    return new SaveItemResult
                    {
                        Error = true,
                        Message = $"Unexpected error: {ex.Message}"
                    };
                }
            }
            catch (SqlException openEx)
            {
                SafeLog(currentUser, "Open Connection Failed (SQL)", openEx.Message, "Item");
                return new SaveItemResult
                {
                    Error = true,
                    Message = GetFriendlySqlMessage(openEx)
                };
            }
            catch (Exception openGenEx)
            {
                SafeLog(currentUser, "Open Connection Failed", openGenEx.Message, "Item");
                return new SaveItemResult
                {
                    Error = true,
                    Message = $"Failed to open connection: {openGenEx.Message}"
                };
            }
        }

        // Compose 'FirstName (Role)' from the current user context; falls back to username if needed.
        private static string BuildReceivedByTag(string? username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return "Unknown (STA)";

                var userRepo = new User_Repository();
                var user = userRepo.GetByUsername(username);

                var first = !string.IsNullOrWhiteSpace(user?.first_name)
                    ? user!.first_name!
                    : ExtractFirstName(username);

                var role = !string.IsNullOrWhiteSpace(user?.role) ? user!.role! : "STA";

                return $"{first} ({role})";
            }
            catch
            {
                return $"{ExtractFirstName(username)} (STA)";
            }
        }

        private static string ExtractFirstName(string nameOrUsername)
        {
            if (string.IsNullOrWhiteSpace(nameOrUsername)) return "Unknown";
            var parts = nameOrUsername.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : nameOrUsername;
        }

        // Map common SQL errors to friendly messages
        private static string GetFriendlySqlMessage(SqlException ex) =>
            ex.Number switch
            {
                1205 => "A database deadlock occurred. Please try again.",
                2627 or 2601 => "A duplicate record conflict occurred.",
                -2 => "The database operation timed out. Please retry.",
                _ => "A database error occurred while saving the item."
            };

        // Best-effort rollback
        private static void TryRollback(SqlTransaction? tx)
        {
            try { tx?.Rollback(); } catch { /* ignore rollback errors */ }
        }

        // Best-effort audit logging
        private static void SafeLog(string user, string action, string description, string entity, string? entityId = null)
        {
            try
            {
                new AuditRepository().LogAction(user, action, description, entity, entityId);
            }
            catch
            {
                // never throw from logging
            }
        }
    }

    public class SaveItemResult
    {
        public int ItemId { get; set; }
        public bool IsNew { get; set; }
        public bool UpdatedExisting { get; set; }
        public int NewQuantity { get; set; }
        public bool Error { get; set; }
        public string? Message { get; set; }
    }
}
