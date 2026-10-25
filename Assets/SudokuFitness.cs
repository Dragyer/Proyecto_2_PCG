using System;

/// <summary>
/// Función de evaluación COMPARTIDA por el algoritmo genético y el recocido simulado.
/// Energía (menor es mejor) = 10 * (soluciones - 1) + |dificultad - objetivo|.
///  - Un puzzle válido tiene 1 solución: no se penaliza.
///  - Cada solución extra suma 10 (se cuentan hasta SolutionCap para tener gradiente).
///  - La diferencia con la dificultad objetivo guía la búsqueda hacia lo que pide el usuario.
/// </summary>
public static class SudokuFitness
{
    public const int SolutionCap = 5;
    public const float UniquenessWeight = 10f;

    public struct Eval { public float energy; public int solutions; public int difficulty; }

    public static Eval Evaluate(int[,] solution, bool[,] mask, int target)
    {
        int[,] grid = new int[9, 9];
        int clues = 0;
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (mask[r, c]) { grid[r, c] = solution[r, c]; clues++; }

        // dificultad: nodos extra de búsqueda del solver MRV
        var s1 = new SudokuSolver(grid);
        s1.CountSolutions(1);
        int difficulty = Math.Max(0, s1.Nodes - (81 - clues) - 1);

        // unicidad: contar soluciones hasta un tope
        int solutions = new SudokuSolver(grid).CountSolutions(SolutionCap);

        float energy = UniquenessWeight * (solutions - 1) + Math.Abs(difficulty - target);
        return new Eval { energy = energy, solutions = solutions, difficulty = difficulty };
    }
}
