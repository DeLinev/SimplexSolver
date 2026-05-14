namespace SimplexMethodApp.Models
{
    public class LinearProgrammingProblem
    {
        public Fraction[] ObjectiveCoefficients { get; init; }
        public Fraction[,] ConstraintCoefficients { get; init; }
        public Fraction[] RightHandSideValues { get; init; }
        public OptimizationType OptimizationType { get; init; }
        public ConstraintSign[] ConstraintSigns { get; init; }

        public int VariableCount => ObjectiveCoefficients.Length;
        public int ConstraintCount => RightHandSideValues.Length;
    }
}
