//-- Add_Slip_Repository.cs --

using che_system.modals.view_model;
using che_system.repositories;
using Microsoft.Data.SqlClient;
using System.Data;

namespace che_system.modals.repositories
{
    public class Add_Slip_Repository : Repository_Base
    {
        public int InsertSlip(Add_Slip_View_Model slip)
        {
            int slipId;

            using (var connection = GetConnection())
            {
                using (var cmd = new SqlCommand("InsertBorrowerWithSlipAndDetails", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

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

                    // === Slip Details ===
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

            return slipId;
        }

    }
}
