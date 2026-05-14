using SimplexMethodApp.Models.Simplex;

namespace SimplexMethodApp.ViewModels.Simplex
{
    public class SolutionStepBuilder
    {
        public static SolutionStepViewModel Build(SimplexStep step)
        {
            return new SolutionStepViewModel
            {
                Title = step.Title,
                Description = step.Description,
                Type = step.Type,
                HasTable = step.Table != null,
                TableRows = step.Table != null ? BuildRows(step.Table) : new(),
                Equations = step.Equations,
            };
        }

        private static List<SimplexTableRowViewModel> BuildRows(SimplexTable table)
        {
            var rows = new List<SimplexTableRowViewModel>();
            int varCount = table.Variables.Count;

            var headerCells = new List<SimplexTableCellViewModel>
            {
                Cell("B",  isHeader: true),
                Cell("Cb", isHeader: true),
                Cell("P",  isHeader: true)
            };
            for (int j = 0; j < varCount; j++)
            {
                string name = table.Variables[j].Name;
                if (j == table.EnteringColumnIndex) name += "↓";
                headerCells.Add(Cell(name, isHeader: true, isEntering: j == table.EnteringColumnIndex));
            }
            headerCells.Add(Cell("Q", isHeader: true));
            rows.Add(new SimplexTableRowViewModel { Cells = headerCells });

            var objCells = new List<SimplexTableCellViewModel>
            {
                Cell(""),
                Cell(""),
                Cell("")
            };
            for (int j = 0; j < varCount; j++)
                objCells.Add(Cell(table.Variables[j].Coefficient.ToString(),
                                  isEntering: j == table.EnteringColumnIndex));
            objCells.Add(Cell(""));
            rows.Add(new SimplexTableRowViewModel { Cells = objCells });

            for (int i = 0; i < table.BasicVariables.Count; i++)
            {
                bool isLeaving = i == table.LeavingRowIndex;
                string basisName = table.BasicVariables[i].Name;
                if (isLeaving) basisName = "← " + basisName;

                var cells = new List<SimplexTableCellViewModel>
                {
                    Cell(basisName, isLeaving: isLeaving),
                    Cell(table.Cb[i].ToString(), isLeaving: isLeaving),
                    Cell(table.Plan[i].ToString(), isLeaving: isLeaving)
                };

                for (int j = 0; j < varCount; j++)
                {
                    bool isPivot = isLeaving && j == table.EnteringColumnIndex;
                    cells.Add(new SimplexTableCellViewModel
                    {
                        Text = table.Matrix[i, j].ToString(),
                        IsPivotElement = isPivot,
                        IsEnteringColumn = j == table.EnteringColumnIndex,
                        IsLeavingRow = isLeaving
                    });
                }

                string qText = table.QValues[i].HasValue
                    ? table.QValues[i]!.Value.ToString()  // Fraction.ToString() вже є
                    : "-";
                cells.Add(Cell(qText, isLeaving: isLeaving));

                rows.Add(new SimplexTableRowViewModel { Cells = cells, IsLeavingRow = isLeaving });
            }

            var estimateCells = new List<SimplexTableCellViewModel>
            {
                Cell("Δⱼ",  isEstimate: true),
                Cell("",     isEstimate: true),
                Cell(table.ObjectiveFunctionValue.ToString(), isEstimate: true)
            };
            for (int j = 0; j < varCount; j++)
                estimateCells.Add(Cell(table.ReducedCosts[j].ToString(),
                                       isEstimate: true,
                                       isEntering: j == table.EnteringColumnIndex));
            estimateCells.Add(Cell("", isEstimate: true));
            rows.Add(new SimplexTableRowViewModel { Cells = estimateCells });

            return rows;
        }

        private static SimplexTableCellViewModel Cell(
        string text,
        bool isHeader = false,
        bool isEstimate = false,
        bool isEntering = false,
        bool isLeaving = false) => new()
        {
            Text = text,
            IsHeader = isHeader,
            IsEstimateRow = isEstimate,
            IsEnteringColumn = isEntering,
            IsLeavingRow = isLeaving
        };
    }
}
