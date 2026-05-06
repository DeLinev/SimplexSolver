using SimplexMethodApp.Models;
using SimplexMethodApp.Models.Simplex;

namespace SimplexMethodApp.Services
{
    public interface ISimplexSolverService
    {
        SimplexSolution Solve(LinearProgrammingProblem problem, CancellationToken token = default);
    }
}
