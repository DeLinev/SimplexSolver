namespace SimplexMethodApp.ViewModels.Simplex
{
    public class SimplexTableCellViewModel
    {
        public string Text { get; init; } = "";
        public bool IsPivotElement { get; init; }
        public bool IsEnteringColumn { get; init; }
        public bool IsLeavingRow { get; init; }
        public bool IsHeader { get; init; }
        public bool IsEstimateRow { get; init; }

        public Color Background => this switch
        {
            { IsPivotElement: true } => Color.FromArgb("#F68048"),
            { IsHeader: true } => Color.FromArgb("#355872"),
            { IsEstimateRow: true } => Color.FromArgb("#A7BECF"),
            { IsEnteringColumn: true } => Color.FromArgb("#d4ebfc"),
            _ => Colors.Transparent
        };

        public Color TextColor => this switch
        {
            { IsHeader: true } => Colors.White,
            { IsPivotElement: true } => Colors.White,
            _ => Color.FromArgb("#111827")
        };

        public FontAttributes FontWeight => this switch
        {
            { IsHeader: true } or { IsEstimateRow: true } or { IsPivotElement: true } => FontAttributes.Bold,
            _ => FontAttributes.None
        };

    }
}
