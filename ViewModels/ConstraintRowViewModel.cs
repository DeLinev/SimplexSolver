using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace SimplexMethodApp.ViewModels
{
    [ObservableObject]
    public partial class ConstraintRowViewModel
    {
        public ObservableCollection<ConstraintCoeffViewModel> Coefficients { get; set; } = new();
        public ObservableCollection<string> SignOptions { get; set; } = new(["≤", "=", "≥"]);
        [ObservableProperty]
        public partial string SelectedSign { get; set; } = "≤";
        [ObservableProperty]
        public partial string RhsValue { get; set; } = "0";
    }
}
