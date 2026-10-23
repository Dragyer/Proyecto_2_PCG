using System;
using System.Collections.Generic;

/// <summary>
/// Generador de Sudoku guiado por heurísticas:
///  1) Solución completa aleatoria con backtracking + MRV.
///  2) Remoción de pistas verificando unicidad con el solver.
///  3) Heurística de dificultad: nodos extra de búsqueda que necesita el solver MRV.
/// </summary>
public static class SudokuGenerator
{
    public const int MinClues = 22; // por debajo es muy difícil encontrar puzzles únicos

    public class Result
    {
        public SudokuBoard Puzzle;
        public int[,] Solution;
        public int Clues;
        public int Difficulty; // 0 = se resuelve sin ramificar; más alto = más difícil
    }

    public static Result Generate(int targetClues, int seed)
    {
        targetClues = Math.Max(MinClues, Math.Min(81, targetClues));
        var rng = new Random(seed);

        // 1) Solución completa
        var empty = new int[SudokuBoard.N, SudokuBoard.N];
        var solver = new SudokuSolver(empty);
        solver.CountSolutions(1, rng);
        int[,] solution = solver.Solution;

        var board = new SudokuBoard();
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                board.Grid[r, c] = solution[r, c];
                board.Given[r, c] = true;
            }

        // 2) Quitar pistas en orden aleatorio manteniendo solución única
        var cells = new List<int>();
        for (int i = 0; i < 81; i++) cells.Add(i);
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cells[i], cells[j]) = (cells[j], cells[i]);
        }

        int clues = 81;
        foreach (int idx in cells)
        {
            if (clues <= targetClues) break;
            int r = idx / 9, c = idx % 9;
            int backup = board.Grid[r, c];
            board.Grid[r, c] = 0;

            var check = new SudokuSolver(board.Grid);
            if (check.CountSolutions(2) == 1)
            {
                board.Given[r, c] = false;
                clues--;
            }
            else board.Grid[r, c] = backup; // quitarla rompe la unicidad
        }

        // 3) Dificultad heurística
        var diff = new SudokuSolver(board.Grid);
        diff.CountSolutions(1);
        int empties = 81 - clues;
        int difficulty = Math.Max(0, diff.Nodes - empties - 1);

        board.RecomputeCandidates();
        return new Result { Puzzle = board, Solution = solution, Clues = clues, Difficulty = difficulty };
    }
}
