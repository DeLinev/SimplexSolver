namespace SimplexMethodApp.ViewModels.Simplex
{
    public class SimplexTableRowViewModel
    {
        public List<SimplexTableCellViewModel> Cells { get; set; }
        public bool IsLeavingRow { get; set; }

        public Color RowBackground => IsLeavingRow
            ? Color.FromArgb("#d4ebfc")
            : Colors.Transparent;
    }
}
