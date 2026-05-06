using SimplexMethodApp.Models.Simplex.Enums;

namespace SimplexMethodApp.Models.Simplex
{
    public class SimplexVariable
    {
        public string Name { get; init; }
        public VariableType Type { get; init; }
        public int Index { get; init; }
        public MValue Coefficient { get; init; }
    }
}
