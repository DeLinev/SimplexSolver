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

        public Color Background => IsPivotElement
            ? Color.FromArgb("#FFE066")
            : IsHeader
                ? Color.FromArgb("#3A86FF")
                : IsEstimateRow
                    ? Color.FromArgb("#EBF3FF")
                    : Colors.Transparent;

        public Color TextColor => IsHeader
            ? Colors.White
            : IsPivotElement
                ? Color.FromArgb("#92400E")
                : Color.FromArgb("#111827");

        public FontAttributes FontWeight =>
            IsHeader || IsEstimateRow || IsPivotElement
                ? FontAttributes.Bold
                : FontAttributes.None;

    }
}
