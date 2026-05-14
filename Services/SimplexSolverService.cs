using SimplexMethodApp.Models;
using SimplexMethodApp.Models.Simplex;
using SimplexMethodApp.Models.Simplex.Enums;
using SimplexMethodApp.Utilities;

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
                    table.QValues = new Fraction?[table.Plan.Length];

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
                    var optimalValue = table.ObjectiveFunctionValue.Regular;

                    Steps.Add(new SimplexStep
                    {
                        Type = StepType.Finish,
                        Title = hasAlternativeOptimum 
                            ? "Знайдено альтернативний оптимум" 
                            : "Оптимальний розв'язок знайдено",
                        Description = hasAlternativeOptimum
                            ? "Усі оцінки задовольняють умову оптимальності, але існує вільна змінна з нульовою оцінкою. Це означає, що задача має альтернативні оптимальні розв'язки."
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
                    table.QValues = new Fraction?[table.Plan.Length];

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

            var newRhs = (Fraction[])problem.RightHandSideValues.Clone();
            var newSigns = (ConstraintSign[])problem.ConstraintSigns.Clone();
            var newCoeffs = (Fraction[,])problem.ConstraintCoefficients.Clone();

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
                    Description = "Позбавляємось від від'ємних значень з правої сторони обмежень шляхом множення рядків на -1.",
                    Equations = BuildNormalizedProblemEquations(normalizedProblem)
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
                Plan = new Fraction[n],
                Matrix = new Fraction[n, totalCols],
                ObjectiveFunctionValue = MValue.Zero,
                ReducedCosts = new MValue[totalCols],
                QValues = new Fraction?[n]
            };

            for (int j = 0; j < origCols; j++)
            {
                simplexTable.Variables.Add(new SimplexVariable
                {
                    Name = $"x{SubscriptConverter.ToSubscript(j + 1)}",
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
                        Name = $"s{SubscriptConverter.ToSubscript(currentSlackIndex + 1)}",
                        Type = VariableType.Slack,
                        Index = currentSlackIndex + 1,
                        Coefficient = MValue.Zero
                    };
                    slackVariables.Add(slackVar);

                    simplexTable.Matrix[i, origCols + currentSlackIndex] = 1;

                    simplexTable.BasicVariables.Add(slackVar);
                    simplexTable.Cb[i] = slackVar.Coefficient;

                    currentSlackIndex++;
                }
                else if (sign == ConstraintSign.GreaterThanOrEqual)
                {
                    var surplusVar = new SimplexVariable
                    {
                        Name = $"s{SubscriptConverter.ToSubscript(currentSlackIndex + 1)}",
                        Type = VariableType.Surplus,
                        Index = currentSlackIndex + 1,
                        Coefficient = MValue.Zero
                    };
                    slackVariables.Add(surplusVar);
                    simplexTable.Matrix[i, origCols + currentSlackIndex] = -1;
                    currentSlackIndex++;

                    var artifVar = new SimplexVariable
                    {
                        Name = $"a{SubscriptConverter.ToSubscript(currentArtifIndex + 1)}",
                        Type = VariableType.Artificial,
                        Index = currentArtifIndex + 1,
                        Coefficient = new MValue(0, -optSign)
                    };
                    artifVariables.Add(artifVar);
                    simplexTable.Matrix[i, origCols + slackCols + currentArtifIndex] = 1;

                    simplexTable.BasicVariables.Add(artifVar);
                    simplexTable.Cb[i] = artifVar.Coefficient;

                    currentArtifIndex++;
                }
                else if (sign == ConstraintSign.Equal)
                {
                    var artifVar = new SimplexVariable
                    {
                        Name = $"a{SubscriptConverter.ToSubscript(currentArtifIndex + 1)}",
                        Type = VariableType.Artificial,
                        Index = currentArtifIndex + 1,
                        Coefficient = new MValue(0, -optSign)
                    };
                    artifVariables.Add(artifVar);
                    simplexTable.Matrix[i, origCols + slackCols + currentArtifIndex] = 1;

                    simplexTable.BasicVariables.Add(artifVar);
                    simplexTable.Cb[i] = artifVar.Coefficient;

                    currentArtifIndex++;
                }
            }

            simplexTable.Variables.AddRange(slackVariables);
            simplexTable.Variables.AddRange(artifVariables);

            string description = artifCols > 0
                ? "Нерівності перетворено на рівності за допомогою балансуючих змінних. " +
                  "Для обмежень типу '≥' та '=' додано штучні змінні з коефіцієнтом " +
                  (optSign == 1 ? "-M" : "+M") + " у цільовій функції (M-метод)."
                : "Нерівності перетворено на рівності за допомогою балансуючих змінних.";

            Steps.Add(new SimplexStep
            {
                Type = StepType.StandardFormConversion,
                Title = "Підготовка задачі до симплекс-методу",
                Description = description,
                Equations = BuildStandardFormEquations(simplexTable, problem.OptimizationType)
            });

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
            Fraction minRatio = Fraction.Zero;

            for (int row = 0; row < table.Matrix.GetLength(0); row++)
            {
                table.QValues[row] = null;

                if (table.Matrix[row, enteringCol] > Fraction.Zero)
                {
                    Fraction ratio = table.Plan[row] / table.Matrix[row, enteringCol];
                    table.QValues[row] = ratio;

                    if (bestRowIndex == null)
                    {
                        minRatio = ratio;
                        bestRowIndex = row;
                    }
                    else if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        bestRowIndex = row;
                    }
                    else if (ratio == minRatio)
                    {
                        var currentVar = table.BasicVariables[row];
                        var bestVar = table.BasicVariables[bestRowIndex.Value];

                        int currentIndex = table.Variables.IndexOf(currentVar);
                        int bestIndex = table.Variables.IndexOf(bestVar);

                        if (currentIndex < bestIndex)
                            bestRowIndex = row;
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
                    && table.Plan[i] > Fraction.Zero)
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
                Plan = table.Plan != null ? (Fraction[])table.Plan.Clone() : Array.Empty<Fraction>(),
                Matrix = table.Matrix != null ? (Fraction[,])table.Matrix.Clone() : new Fraction[0, 0],
                ReducedCosts = table.ReducedCosts != null ? (MValue[])table.ReducedCosts.Clone() : Array.Empty<MValue>(),
                QValues = table.QValues != null ? (Fraction?[])table.QValues.Clone() : Array.Empty<Fraction?>(),

                ObjectiveFunctionValue = table.ObjectiveFunctionValue,
                EnteringColumnIndex = table.EnteringColumnIndex,
                LeavingRowIndex = table.LeavingRowIndex
            };
        }

        private Dictionary<string, Fraction> ExtractSolution(SimplexTable table)
        {
            var solution = new Dictionary<string, Fraction>();

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
                        solution.Add(variable.Name, Fraction.Zero);
                    }
                }
            }

            return solution;
        }

        private void Pivot(SimplexTable table, int enteringCol, int leavingRow)
        {
            var pivotValue = table.Matrix[leavingRow, enteringCol];
            for (int j = 0; j < table.Matrix.GetLength(1); j++)
            {
                table.Matrix[leavingRow, j] /= pivotValue;
            }
            table.Plan[leavingRow] /= pivotValue;

            for (int i = 0; i < table.Matrix.GetLength(0); i++)
            {
                if (i != leavingRow)
                {
                    var factor = table.Matrix[i, enteringCol];
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

        private List<string> BuildNormalizedProblemEquations(LinearProgrammingProblem problem)
        {
            var lines = new List<string>();

            lines.Add(BuildObjectiveLine(
                problem.ObjectiveCoefficients,
                Enumerable.Range(1, problem.VariableCount)
                          .Select(i => $"x{SubscriptConverter.ToSubscript(i)}").ToArray(),
                problem.OptimizationType));

            for (int i = 0; i < problem.ConstraintCount; i++)
            {
                var coeffs = Enumerable.Range(0, problem.VariableCount)
                    .Select(j => problem.ConstraintCoefficients[i, j])
                    .ToArray();
                var varNames = Enumerable.Range(1, problem.VariableCount)
                    .Select(j => $"x{SubscriptConverter.ToSubscript(j)}").ToArray();
                string sign = SignToString(problem.ConstraintSigns[i]);
                string rhs = problem.RightHandSideValues[i].ToString();

                lines.Add($"{BuildTerms(coeffs, varNames)} {sign} {rhs}");
            }

            return lines;
        }

        private List<string> BuildStandardFormEquations(SimplexTable table, OptimizationType optType)
        {
            var lines = new List<string>();
            int rows = table.Matrix.GetLength(0);
            var varNames = table.Variables.Select(v => v.Name).ToArray();

            var objCoeffs = table.Variables.Select(v => v.Coefficient).ToArray();
            lines.Add(BuildMObjectiveLine(objCoeffs, varNames, optType));

            for (int i = 0; i < rows; i++)
            {
                var rowCoeffs = Enumerable.Range(0, table.Variables.Count)
                    .Select(j => table.Matrix[i, j])
                    .ToArray();
                string rhs = table.Plan[i].ToString();
                lines.Add($"{BuildTerms(rowCoeffs, varNames)} = {rhs}");
            }

            return lines;
        }


        private static string BuildObjectiveLine(Fraction[] coeffs, string[] names, OptimizationType optType)
        {
            string direction = optType == OptimizationType.Maximize ? "max" : "min";
            return $"F(x) = {BuildTerms(coeffs, names)} → {direction}";
        }

        private static string BuildMObjectiveLine(MValue[] coeffs, string[] names, OptimizationType optType)
        {
            var terms = new List<string>();
            for (int j = 0; j < coeffs.Length; j++)
            {
                if (coeffs[j].IsZero) continue;

                bool isFirst = terms.Count == 0;
                string mStr = coeffs[j].ToString();
                string varPart = names[j];

                terms.Add(isFirst ? $"{mStr}{varPart}" : $"+ {mStr}{varPart}");
            }

            string direction = optType == OptimizationType.Maximize ? "max" : "min";
            string termsStr = terms.Count > 0 ? string.Join(" ", terms) : "0";
            return $"F(x) = {termsStr} → {direction}";
        }

        private static string BuildTerms(Fraction[] coeffs, string[] names)
        {
            var terms = new List<string>();
            for (int j = 0; j < coeffs.Length; j++)
            {
                Fraction c = coeffs[j];
                if (c.IsZero) continue;

                bool isFirst = terms.Count == 0;
                string varPart = names[j];

                if (isFirst)
                {
                    if (c == Fraction.One) terms.Add(varPart);
                    else if (c == -Fraction.One) terms.Add($"-{varPart}");
                    else terms.Add($"{c}{varPart}");
                }
                else
                {
                    if (c == Fraction.One) terms.Add($"+ {varPart}");
                    else if (c == -Fraction.One) terms.Add($"- {varPart}");
                    else if (c.IsPositive) terms.Add($"+ {c}{varPart}");
                    else terms.Add($"- {(-c)}{varPart}");
                }
            }

            return terms.Count > 0 ? string.Join(" ", terms) : "0";
        }

        private static string FormatNum(double v)
        {
            if (Math.Abs(v) < MValue.Epsilon) return "0";
            return v == Math.Floor(v) ? ((long)v).ToString() : v.ToString("G6");
        }

        private static string SignToString(ConstraintSign sign) => sign switch
        {
            ConstraintSign.LessThanOrEqual => "≤",
            ConstraintSign.GreaterThanOrEqual => "≥",
            _ => "="
        };
    }
}
