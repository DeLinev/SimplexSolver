using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SimplexMethodApp.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        public MainViewModel() 
        {
            InitializeDefaults();    
        }

        public ObservableCollection<string> VariableCountOptions { get; set; }
        public ObservableCollection<string> ConstraintCountOptions { get; set; }
        public ObservableCollection<string> OptimizationOptions { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustomVariableCount))]
        public partial string SelectedVariableOption { get; set; }
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustomConstraintCount))]
        public partial string SelectedConstraintOption { get; set; }

        [ObservableProperty]
        public partial string SelectedOptimizationOption { get; set; }

        public bool IsCustomVariableCount => SelectedVariableOption == "Інше...";
        public bool IsCustomConstraintCount => SelectedConstraintOption == "Інше...";

        [ObservableProperty]
        public partial string CustomVariableCount { get; set; }
        [ObservableProperty]
        public partial string CustomConstraintCount { get; set; }

        public ObservableCollection<ObjTermViewModel> ObjectiveCoefficients { get; } = new();

        private void InitializeDefaults()
        {
            VariableCountOptions = new ObservableCollection<string>(
                Enumerable.Range(2, 9).Select(i => i.ToString()).Concat(new[] { "Інше..." })
            );
            SelectedVariableOption = VariableCountOptions[0];
            CustomVariableCount = "2";

            ConstraintCountOptions = new ObservableCollection<string>(
                Enumerable.Range(2, 9).Select(i => i.ToString()).Concat(new[] { "Інше..." })
            );
            SelectedConstraintOption = ConstraintCountOptions[0];
            CustomConstraintCount = "2";

            OptimizationOptions = new ObservableCollection<string>(["max", "min"]);
            SelectedOptimizationOption = OptimizationOptions[0];

            RebuildObjectiveCoefficients();
        }

        private void RebuildObjectiveCoefficients()
        {
            ObjectiveCoefficients.Clear();

            int count = GetVariableCount();

            for (int i = 0; i < count; i++)
            {
                ObjectiveCoefficients.Add(new ObjTermViewModel
                {
                    VariableLabel = $"x{ToSubscript(i + 1)}",
                    Separator = i < count - 1 ? " + " : ""
                });
            }
        }

        private int GetVariableCount()
        {
            if (SelectedVariableOption == "Інше...")
            {
                if (int.TryParse(CustomVariableCount, out int custom) && custom >= 2)
                    return custom;
                return 2;
            }

            if (int.TryParse(SelectedVariableOption, out int selected))
                return selected;

            return 2;
        }

        private static string ToSubscript(int n)
        {
            var subscripts = new[] { '₀', '₁', '₂', '₃', '₄', '₅', '₆', '₇', '₈', '₉' };
            return string.Concat(n.ToString().Select(c => subscripts[c - '0']));
        }

        partial void OnSelectedVariableOptionChanged(string value)
        {
            RebuildObjectiveCoefficients();
        }

        partial void OnCustomVariableCountChanged(string value)
        {
            if (IsCustomVariableCount)
                RebuildObjectiveCoefficients();
        }
    }
}
