using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Simulated Annealing sobre un puzzle de Sudoku ya generado.
///
/// Estado:      solución fija + máscara de pistas (bool[9,9]).
/// Vecino:      intercambiar una pista por una celda vacía (la cantidad de pistas no cambia).
/// Evaluación:  "energía" (menor es mejor) = penalización por soluciones extra
///              + distancia entre la dificultad del puzzle y la dificultad objetivo.
/// Aceptación:  siempre si mejora; si empeora, con probabilidad exp(-delta / T).
/// </summary>
public class SudokuAnnealer
{
    public struct Config
    {
        public int targetDifficulty;   // dificultad objetivo (nodos extra del solver)
        public float initialTemp;      // temperatura inicial
        public float cooling;          // factor de enfriamiento (0.80 - 0.99)
        public float minTemp;          // temperatura mínima (criterio de parada)
        public int neighborsPerTemp;   // vecinos evaluados en cada nivel de temperatura
        public int seed;               // semilla del azar (misma semilla = comparación justa)
    }

    struct Eval { public float energy; public int solutions; public int difficulty; }

    public const int SolutionCap = 5;          // se cuentan soluciones hasta este tope
    public const float UniquenessWeight = 10f; // costo de cada solución extra

    readonly int[,] solution;
    bool[,] current;
    bool[,] best;

    public bool Stop;
    public bool Finished;
    public float Temperature, StartEnergy, CurrentEnergy, BestEnergy;
    public int StartDifficulty, StartSolutions, BestDifficulty, BestSolutions;
    public int Iterations, Accepted;
    public List<float> History = new List<float>(); // mejor energía por nivel de temperatura

    public SudokuAnnealer(int[,] solution, bool[,] given)
    {
        this.solution = solution;
        current = (bool[,])given.Clone();
    }

    /// <summary>Función de evaluación: es lo que guía la búsqueda.</summary>
    static Eval Evaluate(int[,] solution, bool[,] mask, int target)
    {
        int[,] grid = new int[9, 9];
        int clues = 0;
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (mask[r, c]) { grid[r, c] = solution[r, c]; clues++; }

        // dificultad: nodos extra que necesita el solver MRV
        var s1 = new SudokuSolver(grid);
        s1.CountSolutions(1);
        int difficulty = Math.Max(0, s1.Nodes - (81 - clues) - 1);

        // unicidad: contar soluciones hasta un tope (da gradiente: 2 soluciones es mejor que 5)
        int solutions = new SudokuSolver(grid).CountSolutions(SolutionCap);

        float energy = UniquenessWeight * (solutions - 1) + Math.Abs(difficulty - target);
        return new Eval { energy = energy, solutions = solutions, difficulty = difficulty };
    }

    /// <summary>Vecino: quita una pista y muestra otra celda de la solución.</summary>
    static bool[,] Neighbor(bool[,] m, Random rng)
    {
        bool[,] n = (bool[,])m.Clone();
        int a, b;
        do { a = rng.Next(81); } while (!n[a / 9, a % 9]);   // una pista
        do { b = rng.Next(81); } while (n[b / 9, b % 9]);    // una celda oculta
        n[a / 9, a % 9] = false;
        n[b / 9, b % 9] = true;
        return n;
    }

    static int CountTrue(bool[,] m)
    {
        int n = 0;
        foreach (bool v in m) if (v) n++;
        return n;
    }

    /// <summary>Corrutina: se puede avanzar con MoveNext() un frame a la vez desde la UI.</summary>
    public IEnumerator Run(Config cfg)
    {
        Finished = false; Stop = false;
        Iterations = 0; Accepted = 0;
        History.Clear();
        var rng = new Random(cfg.seed);

        int clues = CountTrue(current);
        Eval e0 = Evaluate(solution, current, cfg.targetDifficulty);
        StartEnergy = CurrentEnergy = BestEnergy = e0.energy;
        StartDifficulty = BestDifficulty = e0.difficulty;
        StartSolutions = BestSolutions = e0.solutions;
        best = (bool[,])current.Clone();
        Temperature = cfg.initialTemp;

        if (clues == 0 || clues == 81) { Finished = true; yield break; }

        var sw = Stopwatch.StartNew();
        while (Temperature > cfg.minTemp && !Stop && BestEnergy > 0f)
        {
            for (int k = 0; k < cfg.neighborsPerTemp && !Stop && BestEnergy > 0f; k++)
            {
                bool[,] nb = Neighbor(current, rng);
                Eval e = Evaluate(solution, nb, cfg.targetDifficulty);
                float delta = e.energy - CurrentEnergy;

                // condición de aceptación de Simulated Annealing
                if (delta <= 0f || rng.NextDouble() < Math.Exp(-delta / Temperature))
                {
                    current = nb;
                    CurrentEnergy = e.energy;
                    Accepted++;
                    if (e.energy < BestEnergy)
                    {
                        BestEnergy = e.energy;
                        BestDifficulty = e.difficulty;
                        BestSolutions = e.solutions;
                        best = (bool[,])nb.Clone();
                    }
                }
                Iterations++;

                if (sw.ElapsedMilliseconds > 8) { yield return null; sw.Restart(); } // ceder el frame
            }
            History.Add(BestEnergy);
            Temperature *= cfg.cooling;
        }
        Finished = true;
    }

    /// <summary>Construye el tablero del mejor estado encontrado.</summary>
    public SudokuBoard BuildBest()
    {
        var b = new SudokuBoard();
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                b.Given[r, c] = best[r, c];
                b.Grid[r, c] = best[r, c] ? solution[r, c] : 0;
            }
        b.RecomputeCandidates();
        return b;
    }
}
