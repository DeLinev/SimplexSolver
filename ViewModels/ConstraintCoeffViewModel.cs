using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimplexMethodApp.ViewModels
{
    [ObservableObject]
    public partial class ConstraintCoeffViewModel
    {
        [ObservableProperty]
        public partial string Value { get; set; } = "0";
        public string VariableLabel { get; init; }
        public string Separator { get; init; }
        public string DisplayLabel => $"{VariableLabel} {Separator} ";
    }
}
