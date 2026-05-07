using SimplexMethodApp.Models.Simplex.Enums;

namespace SimplexMethodApp.ViewModels.Simplex
{
    public class SolutionStepViewModel
    {
        public string Title { get; init; }
        public string Description { get; init; }
        public StepType Type { get; init; }
        public bool HasTable { get; init; }
        public List<SimplexTableRowViewModel> TableRows { get; init; } = new();

        public Color TitleColor => Type switch
        {
            StepType.Finish => Color.FromArgb("#10B981"),
            StepType.Iteration => Color.FromArgb("#3A86FF"),
            _ => Color.FromArgb("#6B7280")
        };

        public string TypeIcon => Type switch
        {
            StepType.Iteration => "🔄",
            StepType.StandardFormConversion => "📐",
            StepType.ArtificialVariablesAdded => "➕",
            StepType.OriginalProblem => "📋",
            StepType.Finish => "✅",
            _ => "•"
        };
    }
}
