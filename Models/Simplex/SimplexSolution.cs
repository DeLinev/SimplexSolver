using SimplexMethodApp.Models.Simplex.Enums;

namespace SimplexMethodApp.Models.Simplex
{
    public class SimplexSolution
    {
        public SolutionStatus Status { get; init; }
        public List<SimplexStep> Steps { get; init; }
        public double? OptimalValue { get; init; }
        public Dictionary<string, double> VariableValues { get; init; }
    }
}
