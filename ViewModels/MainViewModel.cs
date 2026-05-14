using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimplexMethodApp.Models;
using SimplexMethodApp.Models.Simplex.Enums;
using SimplexMethodApp.Models.Simplex;
using SimplexMethodApp.Services;
using SimplexMethodApp.Utilities;
using SimplexMethodApp.Validators;
using SimplexMethodApp.ViewModels.Simplex;
using System.Collections.ObjectModel;

namespace SimplexMethodApp.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private const int MinVariables = 2;
        private const int MaxVariables = 20;
        private const int MinConstraints = 1;
        private const int MaxConstraints = 20;
        private readonly ISimplexSolverService _solver;
        public MainViewModel(ISimplexSolverService solver) 
        {
            _solver = solver;   
        }

        private readonly Debouncer _variableCountDebounce = new();
        private readonly Debouncer _constraintCountDebounce = new();

        public ObservableCollection<string> VariableCountOptions { get; } =
            new(Enumerable.Range(2, 9).Select(i => i.ToString()).Append("Інше..."));

        public ObservableCollection<string> ConstraintCountOptions { get; } =
            new(Enumerable.Range(2, 9).Select(i => i.ToString()).Append("Інше..."));

        public ObservableCollection<string> OptimizationOptions { get; } =
            new(["max", "min"]);


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
        [NotifyDataErrorInfo]
        [PositiveInteger(MinVariables, MaxVariables, "Значення кількості змінних має бути між 2 та 20")]
        [NotifyCanExecuteChangedFor(nameof(SolveCommand))]
        public partial string CustomVariableCount { get; set; }
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [PositiveInteger(MinConstraints, MaxConstraints, "Значення кількості умов-обмежень має бути між 2 та 20")]
        [NotifyCanExecuteChangedFor(nameof(SolveCommand))]
        public partial string CustomConstraintCount { get; set; }

        public ObservableRangeCollection<ObjTermViewModel> ObjectiveCoefficients { get; } = new();
        public ObservableRangeCollection<ConstraintRowViewModel> Constraints { get; } = new();
        public ObservableCollection<string> ValidationErrors { get; } = new();

        public bool HasValidationErrors => ValidationErrors.Count > 0;

        [ObservableProperty]
        public partial bool IsSolutionVisible { get; set; }

        [ObservableProperty]
        public partial string OptimalValue { get; set; }

        [ObservableProperty]
        public partial string OptimalVariables { get; set; }

        [ObservableProperty]
        public partial string SolutionStatusText { get; set; }

        [ObservableProperty]
        public partial Color SolutionStatusColor { get; set; }

        public ObservableCollection<SolutionStepViewModel> SolutionSteps { get; } = new();

        [RelayCommand]
        private async Task Clear()
        {
            InitializeDefaults();
            UpdateValidationErrors();
        }

        [RelayCommand(CanExecute = nameof(CanSolve), IncludeCancelCommand = true)]
        private async Task SolveAsync(CancellationToken token)
        {
            UpdateValidationErrors();
            if (HasValidationErrors)
                return;

            if (!TryBuildProblem(out var problem, out var parseErrors))
            {
                foreach (var e in parseErrors)
                    ValidationErrors.Add(e);
                OnPropertyChanged(nameof(HasValidationErrors));
                return;
            }

            try
            {
                IsBusy = true;
                IsSolutionVisible = false;
                ValidationErrors.Clear();
                OnPropertyChanged(nameof(HasValidationErrors));

                var result = await Task.Run(() => _solver.Solve(problem, token), token);

                PopulateSolution(result);
                IsSolutionVisible = true;
            }
            catch (OperationCanceledException)
            {
                ValidationErrors.Add("Розв'язання було скасовано.");
                OnPropertyChanged(nameof(HasValidationErrors));
            }
            catch (Exception ex)
            {
                ValidationErrors.Add($"Помилка при розв'язанні: {ex.Message}");
                OnPropertyChanged(nameof(HasValidationErrors));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void PopulateSolution(SimplexSolution result)
        {
            SolutionSteps.Clear();

            foreach (var step in result.Steps)
                SolutionSteps.Add(SolutionStepBuilder.Build(step));

            switch (result.Status)
            {
                case SolutionStatus.Optimal:
                case SolutionStatus.AlternateOptimum:
                    SolutionStatusColor = Color.FromArgb("#10B981");
                    OptimalValue = $"F[{SelectedOptimizationOption}] = {result.OptimalValue:G6}";
                    OptimalVariables = string.Join(",  ",
                        result.VariableValues.Select(kv => $"{kv.Key} = {kv.Value:G6}"));
                    break;

                case SolutionStatus.Infeasible:
                    SolutionStatusColor = Color.FromArgb("#EF4444");
                    OptimalValue = "Розв'язку не існує";
                    OptimalVariables = "";
                    break;

                case SolutionStatus.Unbounded:
                    SolutionStatusColor = Color.FromArgb("#F59E0B");
                    OptimalValue = "Функція необмежена";
                    OptimalVariables = "";
                    break;
            }
        }


        private bool CanSolve() => !IsBusy && !HasErrors;

        private void InitializeDefaults()
        {
            SelectedVariableOption = VariableCountOptions[0];
            CustomVariableCount = "2";

            SelectedConstraintOption = ConstraintCountOptions[0];
            CustomConstraintCount = "2";

            SelectedOptimizationOption = OptimizationOptions[0];

            RebuildObjectiveCoefficients();
            RebuildConstraints();
        }

        public async Task InitializeAsync()
        {
            await Task.Delay(50);
            InitializeDefaults();
        }

        private void RebuildObjectiveCoefficients()
        {
            int count = GetVariableCount();
            
            var newObjectiveCoefficients = new List<ObjTermViewModel>();

            for (int i = 0; i < count; i++)
            {
                newObjectiveCoefficients.Add(new ObjTermViewModel
                {
                    VariableLabel = $"x{SubscriptConverter.ToSubscript(i + 1)}",
                    Separator = i < count - 1 ? " + " : ""
                });
            }

            ObjectiveCoefficients.Clear();
            ObjectiveCoefficients.AddRange(newObjectiveCoefficients);
        }

        private void RebuildConstraints()
        {
            int variableCount = GetVariableCount();
            int constraintCount = GetConstraintCount();

            var newConstraints = new List<ConstraintRowViewModel>();

            for (int i = 0; i < constraintCount; i++)
            {
                var row = new ConstraintRowViewModel
                {
                    Coefficients = new ObservableCollection<ConstraintCoeffViewModel>()
                };
                for (int j = 0; j < variableCount; j++)
                {
                    row.Coefficients.Add(new ConstraintCoeffViewModel
                    {
                        VariableLabel = $"x{SubscriptConverter.ToSubscript(j + 1)}",
                        Separator = j < variableCount - 1 ? " + " : ""
                    });
                }

                newConstraints.Add(row);
            }

            Constraints.Clear();
            Constraints.AddRange(newConstraints);
        }

        private void UpdateValidationErrors()
        {
            ValidationErrors.Clear();

            foreach (var error in GetErrors())
            {
                if (error.ErrorMessage != null)
                    ValidationErrors.Add(error.ErrorMessage);
            }

            OnPropertyChanged(nameof(HasValidationErrors));
            SolveCommand.NotifyCanExecuteChanged();
        }

        private bool TryBuildProblem(out LinearProgrammingProblem problem, out List<string> errorMessages)
        {
            errorMessages = new List<string>();
            problem = null;

            var objectiveCoeffs = new Fraction[ObjectiveCoefficients.Count];
            for (int i = 0; i < objectiveCoeffs.Length; i++)
            {
                if (!Fraction.TryParse(ObjectiveCoefficients[i].Value, out objectiveCoeffs[i]))
                {
                    errorMessages.Add($"Невірний коефіцієнт цільової функції: x{SubscriptConverter.ToSubscript(i + 1)}");
                }
            }

            var (m, n) = (Constraints.Count, ObjectiveCoefficients.Count); 
            var constraintCoeffs = new Fraction[m, n];
            var rgsValues = new Fraction[m];
            var signs = new ConstraintSign[m];

            for (int i = 0; i < Constraints.Count; i++)
            {
                var row = Constraints[i];
                for (int j = 0; j < ObjectiveCoefficients.Count; j++)
                {
                    if (!Fraction.TryParse(row.Coefficients[j].Value, out constraintCoeffs[i, j]))
                    {
                        errorMessages.Add($"Невірний коефіцієнт в умові {i + 1}: x{SubscriptConverter.ToSubscript(j + 1)}");
                    }
                }

                if (!Fraction.TryParse(row.RhsValue, out rgsValues[i]))
                {
                    errorMessages.Add($"Невірне праве значення в умові {i + 1}");
                }

                try
                {
                    signs[i] = row.SelectedSign switch
                    {
                        "≤" => ConstraintSign.LessThanOrEqual,
                        "=" => ConstraintSign.Equal,
                        "≥" => ConstraintSign.GreaterThanOrEqual,
                        _ => throw new InvalidOperationException("Невідомий знак обмеження")
                    }; 
                } 
                catch (InvalidOperationException e)
                {
                    errorMessages.Add($"Невірний знак обмеження в умові {i + 1}");
                }
            }

            if (errorMessages.Count > 0)
                return false;

            problem = new LinearProgrammingProblem
            {
                ObjectiveCoefficients = objectiveCoeffs,
                ConstraintCoefficients = constraintCoeffs,
                RightHandSideValues = rgsValues,
                ConstraintSigns = signs,
                OptimizationType = SelectedOptimizationOption == "max" 
                                    ? OptimizationType.Maximize 
                                    : OptimizationType.Minimize
            };

            return true;
        }

        private int GetVariableCount()
        {
            if (SelectedVariableOption == "Інше...")
            {
                if (int.TryParse(CustomVariableCount, out int custom) && custom >= MinVariables && custom <= MaxVariables)
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
                if (int.TryParse(CustomConstraintCount, out int custom) && custom >= MinConstraints && custom <= MaxConstraints)
                    return custom;
                return 2;
            }
            if (int.TryParse(SelectedConstraintOption, out int selected))
                return selected;
            return 2;
        }

        partial void OnSelectedVariableOptionChanged(string value)
        {
            RebuildObjectiveCoefficients();
            RebuildConstraints();
        }

        partial void OnCustomVariableCountChanged(string value)
        {
            if (!IsCustomVariableCount) return;

            _variableCountDebounce.Run(() =>
            {
                RebuildObjectiveCoefficients();
                RebuildConstraints();
                UpdateValidationErrors();
            });
        }

        partial void OnSelectedConstraintOptionChanged(string value)
        {
            RebuildConstraints();
        }

        partial void OnCustomConstraintCountChanged(string value)
        {
            if (!IsCustomConstraintCount) return;

            _constraintCountDebounce.Run(() =>
            {
                RebuildConstraints();
                UpdateValidationErrors();
            });
        }
    }
}
