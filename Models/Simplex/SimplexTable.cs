namespace SimplexMethodApp.Models.Simplex
{
    public class SimplexTable
    {
        public List<SimplexVariable> Variables { get; init; }
        public List<SimplexVariable> BasicVariables { get; set; }
        public MValue[] Cb { get; set; }
        public double[] Plan { get; set; }
        public double[,] Matrix { get; set; }
        public MValue ObjectiveFunctionValue { get; set; }
        public MValue[] ReducedCosts { get; set; }
        public double?[] QValues { get; set; }
        public int? EnteringColumnIndex { get; set; }
        public int? LeavingRowIndex { get; set; }
    }
}
