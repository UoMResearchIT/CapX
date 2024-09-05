namespace PPMTool.Data.Entities
{
    public class FinancialReference
    {
        public int FinancialReferenceId { get; set; }

        public int FinancialYear { get; set; }

        public float Grade41Costs { get; set; }

        public float Grade55Costs { get; set; }

        public float Grade65Costs { get; set; }

        public float Grade71Costs { get; set; }

        public float Grade75Costs { get; set; }

        public float RecoveryTarget { get; set; }
    }
}
