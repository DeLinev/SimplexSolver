using SimplexMethodApp.Models.Simplex.Enums;

namespace SimplexMethodApp.Models.Simplex
{
    public class SimplexStep
    {
        public  StepType Type { get; init; }
        public string Title { get; init; }
        public string Description { get; init; }
        public SimplexTable? Table { get; init; }
        public bool HasTable => Table != null;
    }
}
