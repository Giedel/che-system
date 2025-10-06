//-- Reports_Repository.cs --

using che_system.modals.model;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;

namespace che_system.repositories
{
    public partial class Reports_Repository : Repository_Base
    {
        public ObservableCollection<MonthlyChemicalUsageModel> GetMonthlyChemicalUsage(int month, int year)
        {
            var result = new ObservableCollection<MonthlyChemicalUsageModel>();

            using var connection = GetConnection();
            connection.Open();

            string query = @"
        SELECT 
            i.name AS ChemicalName,
            SUM(sd.quantity_released) AS TotalConsumption
        FROM Slip_Detail sd
        INNER JOIN Item i ON sd.item_id = i.item_id
        WHERE 
            i.category = 'Chemical' AND
            sd.date_released IS NOT NULL AND
            MONTH(sd.date_released) = @Month AND
            YEAR(sd.date_released) = @Year
        GROUP BY i.name
        ORDER BY TotalConsumption DESC;";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Month", month);
            command.Parameters.AddWithValue("@Year", year);

            using var reader = command.ExecuteReader();

            int rank = 1;
            while (reader.Read())
            {
                result.Add(new MonthlyChemicalUsageModel
                {
                    Rank = rank++,
                    ChemicalName = reader["ChemicalName"].ToString(),
                    TotalConsumption = Convert.ToInt32(reader["TotalConsumption"])
                });
            }

            return result;
        }
    }
}
