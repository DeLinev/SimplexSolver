using SimplexMethodApp.Models.Simplex.Enums;

namespace SimplexMethodApp.Models.Simplex
{
    public class SimplexStep
    {
        public StepType Type { get; init; }
        public string Title { get; init; }
        public string Description { get; init; }
        public SimplexTable? Table { get; init; }
        public List<string> Equations {  get; init; } = [];
        public bool HasEquations => Equations.Count > 0;
        public bool HasTable => Table != null;
    }
}
