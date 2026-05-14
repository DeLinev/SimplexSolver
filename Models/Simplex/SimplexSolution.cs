using SimplexMethodApp.Models.Simplex.Enums;

namespace SimplexMethodApp.Models.Simplex
{
    public class SimplexSolution
    {
        public SolutionStatus Status { get; init; }
        public List<SimplexStep> Steps { get; init; }
        public Fraction? OptimalValue { get; init; }
        public Dictionary<string, Fraction> VariableValues { get; init; }
    }
}
