namespace SimplexMethodApp.Models
{
    public class LinearProgrammingProblem
    {
        public double[] ObjectiveCoefficients { get; init; }
        public double[,] ConstraintCoefficients { get; init; }
        public double[] RightHandSideValues { get; init; }
        public OptimizationType OptimizationType { get; init; }
        public ConstraintSign[] ConstraintSigns { get; init; }

        public int VariableCount => ObjectiveCoefficients.Length;
        public int ConstraintCount => RightHandSideValues.Length;
    }
}
