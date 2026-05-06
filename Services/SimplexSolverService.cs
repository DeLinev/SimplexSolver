using SimplexMethodApp.Models;
using SimplexMethodApp.Models.Simplex;
using SimplexMethodApp.Models.Simplex.Enums;

namespace SimplexMethodApp.Services
{
    public class SimplexSolverService : ISimplexSolverService
    {
        public List<SimplexStep> Steps { get; init; } = new();

        public SimplexSolution Solve(LinearProgrammingProblem problem, CancellationToken token = default)
        {
            Steps.Clear();
            int optSign = problem.OptimizationType == OptimizationType.Maximize ? 1 : -1;
            var normalizedProblem = NormalizeRhs(problem);
            var table = ConvertToStandardForm(normalizedProblem, optSign);
            RecalculateTable(table);

            int iteration = 1;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                int? enteringCol = FindEnteringColumn(table, optSign);

                if (enteringCol == null)
                {
                    table.EnteringColumnIndex = null;
                    table.LeavingRowIndex = null;

                    if (HasArtificialVariablesInBasis(table))
                    {
                        Steps.Add(new SimplexStep
                        {
                            Type = StepType.Finish,
                            Title = "Розв'язку не існує",
                            Description = "Штучна змінна залишилась у базисі з ненульовим " +
                                      "значенням. Система обмежень несумісна.",
                            Table = CloneTable(table)
                        });

                        // No feasible solution exists
                        return new SimplexSolution
                        {
                            Status = SolutionStatus.Infeasible,
                            Steps = [.. Steps]
                        };
                    }

                    bool hasAlternativeOptimum = false;
                    for (int j = 0; j < table.Variables.Count; j++)
                    {
                        var variable = table.Variables[j];

                        if (variable.Type != VariableType.Artificial && !table.BasicVariables.Contains(variable))
                        {
                            if (table.ReducedCosts[j].IsZero)
                            {
                                hasAlternativeOptimum = true;
                                break;
                            }
                        }
                    }

                    var solution = ExtractSolution(table);
                    double optimalValue = table.ObjectiveFunctionValue.Regular;

                    Steps.Add(new SimplexStep
                    {
                        Type = StepType.Finish,
                        Title = hasAlternativeOptimum 
                            ? "Знайдено альтернативний оптимум" 
                            : "Оптимальний розв'язок знайдено",
                        Description = hasAlternativeOptimum
                            ? "Усі оцінки задовольняють умову оптимальності, але існує вільна змінна з нульовою оцінкою. Це означає, що задача має безліч оптимальних розв'язків (альтернативний оптимум)."
                            : "Усі оцінки задовольняють умову оптимальності. Знайдено єдиний оптимальний план задачі.",
                        Table = CloneTable(table)
                    });

                    // Optimal solution found
                    return new SimplexSolution
                    {
                        Status = hasAlternativeOptimum 
                            ? SolutionStatus.AlternateOptimum 
                            : SolutionStatus.Optimal,
                        Steps = Steps,
                        OptimalValue = optimalValue,
                        VariableValues = solution
                    };
                }

                int? leavingRow = FindLeavingRow(table, enteringCol.Value);

                if (leavingRow == null)
                {
                    table.EnteringColumnIndex = enteringCol;
                    table.LeavingRowIndex = null;

                    Steps.Add(new SimplexStep
                    {
                        Type = StepType.Finish,
                        Title = "Функція необмежена",
                        Description = $"Всі елементи вхідного стовпця {table.Variables[enteringCol.Value].Name} ≤ 0. " +
                              "Цільова функція не має скінченного оптимуму (може зростати/спадати до нескінченності).",
                        Table = CloneTable(table)
                    });

                    // Unbounded solution
                    return new SimplexSolution()
                    {
                        Status = SolutionStatus.Unbounded,
                        Steps = Steps
                    };
                }

                table.EnteringColumnIndex = enteringCol;
                table.LeavingRowIndex = leavingRow;

                Steps.Add(new SimplexStep
                {
                    Type = StepType.Iteration,
                    Title = $"Ітерація {iteration}",
                    Description = $"Вибір вхідного стовпця: {table.Variables[enteringCol.Value].Name}, " +
                                  $"вихідного рядка: {table.BasicVariables[leavingRow.Value].Name}.",
                    Table = CloneTable(table)
                });

                Pivot(table, enteringCol.Value, leavingRow.Value);
                RecalculateTable(table);

                iteration++;
            }
        }

        private LinearProgrammingProblem NormalizeRhs(LinearProgrammingProblem problem)
        {
            bool hasNegativeRhs = false;

            var newRhs = (double[])problem.RightHandSideValues.Clone();
            var newSigns = (ConstraintSign[])problem.ConstraintSigns.Clone();
            var newCoeffs = (double[,])problem.ConstraintCoefficients.Clone();

            for (int i = 0; i < problem.ConstraintCount; i++)
            {
                if (newRhs[i] < 0)
                {
                    hasNegativeRhs = true;
                    newRhs[i] = -newRhs[i];

                    newSigns[i] = newSigns[i] switch
                    {
                        ConstraintSign.LessThanOrEqual => ConstraintSign.GreaterThanOrEqual,
                        ConstraintSign.GreaterThanOrEqual => ConstraintSign.LessThanOrEqual,
                        _ => newSigns[i]
                    };

                    for (int j = 0; j < problem.VariableCount; j++)
                    {
                        newCoeffs[i, j] = -newCoeffs[i, j];
                    }
                }
            }

            var normalizedProblem = new LinearProgrammingProblem
            {
                ObjectiveCoefficients = problem.ObjectiveCoefficients,
                ConstraintCoefficients = newCoeffs,
                RightHandSideValues = newRhs,
                ConstraintSigns = newSigns,
                OptimizationType = problem.OptimizationType
            };

            if (hasNegativeRhs)
            {
                Steps.Add(new SimplexStep
                {
                    Type = StepType.OriginalProblem,
                    Title = "Нормалізація правої частини",
                    Description = "Позбавляємось від від'ємних значень з правої сторони обмежень шляхом множення рядків на -1."
                });
            }

            return normalizedProblem;
        }

        private SimplexTable ConvertToStandardForm(LinearProgrammingProblem problem, int optSign)
        {
            int n = problem.ConstraintCount;
            int origCols = problem.VariableCount;

            int slackCols = problem.ConstraintSigns.Count(s =>
                s == ConstraintSign.LessThanOrEqual ||
                s == ConstraintSign.GreaterThanOrEqual);

            int artifCols = problem.ConstraintSigns.Count(s =>
                s == ConstraintSign.GreaterThanOrEqual ||
                s == ConstraintSign.Equal);

            int totalCols = origCols + slackCols + artifCols;

            var simplexTable = new SimplexTable
            {
                Variables = new List<SimplexVariable>(),
                BasicVariables = new List<SimplexVariable>(),
                Cb = new MValue[n],
                Plan = new double[n],
                Matrix = new double[n, totalCols],
                ObjectiveFunctionValue = MValue.Zero,
                ReducedCosts = new MValue[totalCols],
                QValues = new double?[n]
            };

            for (int j = 0; j < origCols; j++)
            {
                simplexTable.Variables.Add(new SimplexVariable
                {
                    Name = $"x{ToSubscript(j + 1)}",
                    Type = VariableType.Original,
                    Index = j + 1,
                    Coefficient = problem.ObjectiveCoefficients[j]
                });
            }

            var slackVariables = new List<SimplexVariable>();
            var artifVariables = new List<SimplexVariable>();

            int currentSlackIndex = 0;
            int currentArtifIndex = 0;

            for (int i = 0; i < n; i++)
            {
                simplexTable.Plan[i] = problem.RightHandSideValues[i];

                for (int j = 0; j < origCols; j++)
                {
                    simplexTable.Matrix[i, j] = problem.ConstraintCoefficients[i, j];
                }

                var sign = problem.ConstraintSigns[i];

                if (sign == ConstraintSign.LessThanOrEqual)
                {
                    var slackVar = new SimplexVariable
                    {
                        Name = $"s{ToSubscript(currentSlackIndex + 1)}",
                        Type = VariableType.Slack,
                        Index = currentSlackIndex + 1,
                        Coefficient = MValue.Zero
                    };
                    slackVariables.Add(slackVar);

                    simplexTable.Matrix[i, origCols + currentSlackIndex] = 1.0;

                    simplexTable.BasicVariables.Add(slackVar);
                    simplexTable.Cb[i] = slackVar.Coefficient;

                    currentSlackIndex++;
                }
                else if (sign == ConstraintSign.GreaterThanOrEqual)
                {
                    var surplusVar = new SimplexVariable
                    {
                        Name = $"s{ToSubscript(currentSlackIndex + 1)}",
                        Type = VariableType.Surplus,
                        Index = currentSlackIndex + 1,
                        Coefficient = MValue.Zero
                    };
                    slackVariables.Add(surplusVar);
                    simplexTable.Matrix[i, origCols + currentSlackIndex] = -1.0;
                    currentSlackIndex++;

                    var artifVar = new SimplexVariable
                    {
                        Name = $"a{ToSubscript(currentArtifIndex + 1)}",
                        Type = VariableType.Artificial,
                        Index = currentArtifIndex + 1,
                        Coefficient = new MValue(0, -optSign)
                    };
                    artifVariables.Add(artifVar);
                    simplexTable.Matrix[i, origCols + slackCols + currentArtifIndex] = 1.0;

                    simplexTable.BasicVariables.Add(artifVar);
                    simplexTable.Cb[i] = artifVar.Coefficient;

                    currentArtifIndex++;
                }
                else if (sign == ConstraintSign.Equal)
                {
                    var artifVar = new SimplexVariable
                    {
                        Name = $"a{ToSubscript(currentArtifIndex + 1)}",
                        Type = VariableType.Artificial,
                        Index = currentArtifIndex + 1,
                        Coefficient = new MValue(0, -optSign)
                    };
                    artifVariables.Add(artifVar);
                    simplexTable.Matrix[i, origCols + slackCols + currentArtifIndex] = 1.0;

                    simplexTable.BasicVariables.Add(artifVar);
                    simplexTable.Cb[i] = artifVar.Coefficient;

                    currentArtifIndex++;
                }
            }

            simplexTable.Variables.AddRange(slackVariables);
            simplexTable.Variables.AddRange(artifVariables);

            Steps.Add(new SimplexStep
            {
                Type = StepType.StandardFormConversion,
                Title = "Приведення до стандартної форми",
                Description = "Додано балансуючі змінні для перетворення нерівностей у рівності.",
                Table = simplexTable
            });

            if (artifCols > 0)
            {
                Steps.Add(new SimplexStep
                {
                    Type = StepType.ArtificialVariablesAdded,
                    Title = "Додавання штучних змінних",
                    Description = "Для обмежень типу '≥' та '=' додано штучні змінні.",
                    Table = simplexTable
                });
            }

            return simplexTable;
        }

        private void RecalculateTable(SimplexTable table)
        {
            int rows = table.Matrix.GetLength(0);
            int cols = table.Matrix.GetLength(1);

            table.ObjectiveFunctionValue = MValue.Zero;
            for (int j = 0; j < cols; j++)
            {
                table.ReducedCosts[j] = MValue.Zero;
            }

            for (int i = 0; i < rows; i++)
            {
                table.ObjectiveFunctionValue += table.Cb[i] * table.Plan[i];
            }

            for (int j = 0; j < cols; j++)
            {
                MValue sum = MValue.Zero;

                for (int i = 0; i < rows; i++)
                {
                    sum += table.Cb[i] * table.Matrix[i, j];
                }

                table.ReducedCosts[j] = sum - table.Variables[j].Coefficient;
            }
        }

        private int? FindEnteringColumn(SimplexTable table, int optSign)
        {
            int? bestColumnIndex = null;
            MValue minCost = MValue.Zero;

            for (int j = 0; j < table.ReducedCosts.Length; j++)
            {
                MValue normalizedCost = table.ReducedCosts[j] * optSign;

                if (normalizedCost.IsNegative)
                {
                    if (bestColumnIndex == null || normalizedCost < minCost)
                    {
                        minCost = normalizedCost;
                        bestColumnIndex = j;
                    }
                }
            }

            return bestColumnIndex;
        }

        private int? FindLeavingRow(SimplexTable table, int enteringCol)
        {
            int? bestRowIndex = null;
            double? minRatio = null;

            for (int row = 0; row < table.Matrix.GetLength(0); row++)
            {
                table.QValues[row] = null;

                if (table.Matrix[row, enteringCol] > MValue.Epsilon)
                {
                    double ratio = table.Plan[row] / table.Matrix[row, enteringCol];
                    table.QValues[row] = ratio;

                    if (bestRowIndex == null)
                    {
                        minRatio = ratio;
                        bestRowIndex = row;
                    }
                    else if (ratio < minRatio.Value - MValue.Epsilon)
                    {
                        minRatio = ratio;
                        bestRowIndex = row;
                    }
                    else if (Math.Abs(ratio - minRatio.Value) <= MValue.Epsilon)
                    {
                        var currentVar = table.BasicVariables[row];
                        var bestVar = table.BasicVariables[bestRowIndex.Value];

                        int currentIndex = table.Variables.IndexOf(currentVar);
                        int bestIndex = table.Variables.IndexOf(bestVar);

                        if (currentIndex < bestIndex)
                        {
                            bestRowIndex = row;
                        }
                    }
                }
            }

            return bestRowIndex;
        }

        private bool HasArtificialVariablesInBasis(SimplexTable table)
        {
            for (int i = 0; i < table.BasicVariables.Count; i++)
            {
                if (table.BasicVariables[i].Type == VariableType.Artificial 
                    && table.Plan[i] > MValue.Epsilon)
                {
                    return true;
                }
            }

            return false;
        }

        private SimplexTable CloneTable(SimplexTable table)
        {
            return new SimplexTable
            {
                Variables = [.. table.Variables],
                BasicVariables = [.. table.BasicVariables],

                Cb = table.Cb != null ? (MValue[])table.Cb.Clone() : Array.Empty<MValue>(),
                Plan = table.Plan != null ? (double[])table.Plan.Clone() : Array.Empty<double>(),
                Matrix = table.Matrix != null ? (double[,])table.Matrix.Clone() : new double[0, 0],
                ReducedCosts = table.ReducedCosts != null ? (MValue[])table.ReducedCosts.Clone() : Array.Empty<MValue>(),
                QValues = table.QValues != null ? (double?[])table.QValues.Clone() : Array.Empty<double?>(),

                ObjectiveFunctionValue = table.ObjectiveFunctionValue,
                EnteringColumnIndex = table.EnteringColumnIndex,
                LeavingRowIndex = table.LeavingRowIndex
            };
        }

        private Dictionary<string, double> ExtractSolution(SimplexTable table)
        {
            var solution = new Dictionary<string, double>();

            foreach (var variable in table.Variables)
            {
                if (variable.Type == VariableType.Original)
                {
                    int basicIndex = table.BasicVariables.IndexOf(variable);

                    if (basicIndex != -1)
                    {
                        solution.Add(variable.Name, table.Plan[basicIndex]);
                    }
                    else
                    {
                        solution.Add(variable.Name, 0.0);
                    }
                }
            }

            return solution;
        }

        private void Pivot(SimplexTable table, int enteringCol, int leavingRow)
        {
            double pivotValue = table.Matrix[leavingRow, enteringCol];
            for (int j = 0; j < table.Matrix.GetLength(1); j++)
            {
                table.Matrix[leavingRow, j] /= pivotValue;
            }
            table.Plan[leavingRow] /= pivotValue;

            for (int i = 0; i < table.Matrix.GetLength(0); i++)
            {
                if (i != leavingRow)
                {
                    double factor = table.Matrix[i, enteringCol];
                    for (int j = 0; j < table.Matrix.GetLength(1); j++)
                    {
                        table.Matrix[i, j] -= (factor * table.Matrix[leavingRow, j]);
                    }
                    table.Plan[i] -= (factor * table.Plan[leavingRow]);
                }
            }

            table.BasicVariables[leavingRow] = table.Variables[enteringCol];
            table.Cb[leavingRow] = table.BasicVariables[leavingRow].Coefficient;
        }

        public static string ToSubscript(int n)
        {
            var subscripts = new[] { '₀', '₁', '₂', '₃', '₄', '₅', '₆', '₇', '₈', '₉' };
            return string.Concat(n.ToString().Select(c => subscripts[c - '0']));
        }
    }
}
