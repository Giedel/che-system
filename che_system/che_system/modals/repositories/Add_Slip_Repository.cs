//-- Add_Slip_Repository.cs --

using che_system.modals.view_model;
using che_system.repositories;
using Microsoft.Data.SqlClient;
using System.Data;

namespace che_system.modals.repositories
{
    public class Add_Slip_Repository : Repository_Base
    {
        public int InsertSlip(Add_Slip_View_Model slip, string currentUser)
        {
            int slipId;

            using (var connection = GetConnection())
            {
                using (var cmd = new SqlCommand("InsertBorrowerWithSlipAndDetails", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // === Borrower Info ===
                    cmd.Parameters.AddWithValue("@name", slip.Name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@subject_title", slip.SubjectTitle ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@subject_code", slip.SubjectCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@class_schedule", slip.ClassSchedule ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@instructor", slip.Instructor ?? (object)DBNull.Value);

                    // === Slip Info ===
                    cmd.Parameters.AddWithValue("@date_filed", slip.DateFiled);
                    cmd.Parameters.AddWithValue("@date_of_use", slip.DateOfUse == default ? (object)DBNull.Value : slip.DateOfUse);
                    cmd.Parameters.AddWithValue("@received_by", slip.ReceivedBy ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@remarks", slip.Remarks ?? (object)DBNull.Value);

                    // === Slip Details TVP ===
                    var detailsTable = new DataTable();
                    detailsTable.Columns.Add("item_id", typeof(int));
                    detailsTable.Columns.Add("quantity_borrowed", typeof(int));
                    detailsTable.Columns.Add("remarks", typeof(string));

                    foreach (var detail in slip.SlipDetails)
                    {
                        detailsTable.Rows.Add(detail.ItemId, detail.QuantityBorrowed, detail.Remarks ?? (object)DBNull.Value);
                    }

                    var detailsParam = cmd.Parameters.AddWithValue("@details", detailsTable);
                    detailsParam.SqlDbType = SqlDbType.Structured;
                    detailsParam.TypeName = "SlipDetailTableType";

                    connection.Open();
                    slipId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            // ✅ Log creation of slip
            try
            {
                new AuditRepository().LogAction(
                    currentUser,
                    "Add Slip",
                    $"Added new borrower slip for '{slip.Name}' (Slip ID: {slipId}, Subject: {slip.SubjectTitle}).",
                    "Borrower_Slip",
                    slipId.ToString()
                );
            }
            catch (Exception logEx)
            {
                // Optional: silently handle logging errors (do not stop main flow)
                Console.WriteLine($"[Audit Log Failed] {logEx.Message}");
            }

            return slipId;
        }

        // Second-phase update for proof image (if proc not yet extended)
        public void UpdateSlipProofImage(int slipId, byte[] imageBytes, string fileName, string contentType)
        {
            using var connection = GetConnection();
            using var cmd = new SqlCommand(@"
UPDATE Borrower_Slip
SET proof_image = @img,
    proof_image_file_name = @fn,
    proof_image_content_type = @ct
WHERE slip_id = @id;", connection);

            cmd.Parameters.AddWithValue("@id", slipId);
            cmd.Parameters.AddWithValue("@img", imageBytes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@fn", string.IsNullOrEmpty(fileName) ? (object)DBNull.Value : fileName);
            cmd.Parameters.AddWithValue("@ct", string.IsNullOrEmpty(contentType) ? (object)DBNull.Value : contentType);

            connection.Open();
            cmd.ExecuteNonQuery();
        }
    }
}
