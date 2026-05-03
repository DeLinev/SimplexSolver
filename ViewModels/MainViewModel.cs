using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.PlatformConfiguration;
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
        public ObservableCollection<ConstraintRowViewModel> Constraints { get; } = new();

        [RelayCommand]
        private async Task Clear()
        {
            InitializeDefaults();
        }

        [RelayCommand(CanExecute = nameof(CanSolve), IncludeCancelCommand = true)]
        private async Task SolveAsync(CancellationToken token)
        {
            try
            {
                IsBusy = true;
                //ErrorMessage = null;
                //SolutionSteps.Clear();

                //var result = await Task.Run(() => _solver.Solve(/* параметри */), token);
                // заповнення SolutionSteps, OptimalValueText тощо
                await Task.Delay(3000, token);

            }
            catch (OperationCanceledException)
            {
                //ErrorMessage = "Обчислення скасовано";
            }
            catch (Exception ex)
            {
                //ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSolve() => !IsBusy;

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
            RebuildConstraints();
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

        private void RebuildConstraints()
        {
            Constraints.Clear();
            int variableCount = GetVariableCount();
            int constraintCount = GetConstraintCount();
            for (int i = 0; i < constraintCount; i++)
            {
                var row = new ConstraintRowViewModel
                {
                    Coefficients = new ObservableCollection<ConstraintCoeffViewModel>(),
                };
                for (int j = 0; j < variableCount; j++)
                {
                    row.Coefficients.Add(new ConstraintCoeffViewModel
                    {
                        VariableLabel = $"x{ToSubscript(j + 1)}",
                        Separator = j < variableCount - 1 ? " + " : ""
                    });
                }
                Constraints.Add(row);
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

        private int GetConstraintCount()
        {
            if (SelectedConstraintOption == "Інше...")
            {
                if (int.TryParse(CustomConstraintCount, out int custom) && custom >= 2)
                    return custom;
                return 2;
            }
            if (int.TryParse(SelectedConstraintOption, out int selected))
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
            RebuildConstraints();
        }

        partial void OnCustomVariableCountChanged(string value)
        {
            if (IsCustomVariableCount)
            {
                RebuildObjectiveCoefficients();
                RebuildConstraints();
            }
        }

        partial void OnSelectedConstraintOptionChanged(string value)
        {
            RebuildConstraints();
        }

        partial void OnCustomConstraintCountChanged(string value)
        {
            if (IsCustomConstraintCount)
                RebuildConstraints();
        }
    }
}
