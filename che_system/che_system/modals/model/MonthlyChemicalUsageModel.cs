//-- MonthlyChemicalUsageModel.cs --

namespace che_system.modals.model
{
    public class MonthlyChemicalUsageModel
    {
        public int Rank { get; set; }
        public string? ChemicalName { get; set; }
        public int TotalConsumption { get; set; }

        // NEW: unit and merged display
        public string? Unit { get; set; }
        public string TotalConsumptionWithUnit => string.IsNullOrWhiteSpace(Unit)
            ? TotalConsumption.ToString()
            : $"{TotalConsumption} {Unit}";
    }
}
