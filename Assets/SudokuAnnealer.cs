using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Búsqueda local: Simulated Annealing sobre un puzzle ya generado.
/// Estado: solución fija + máscara de pistas. Vecino: intercambiar una pista por una celda oculta.
/// Evaluación: SudokuFitness (compartida con el algoritmo genético).
/// Aceptación: siempre si mejora; si empeora, con probabilidad exp(-delta / T).
/// </summary>
public class SudokuAnnealer
{
    public struct Config
    {
        public int targetDifficulty;
        public float initialTemp;
        public float cooling;
        public float minTemp;
        public int neighborsPerTemp;
        public int seed;
    }

    readonly int[,] solution;
    bool[,] current;
    bool[,] best;

    public bool Stop;
    public bool Finished;
    public float Temperature, StartEnergy, CurrentEnergy, BestEnergy;
    public int StartDifficulty, StartSolutions, BestDifficulty, BestSolutions;
    public int Iterations, Accepted;
    public List<float> History = new List<float>();

    public SudokuAnnealer(int[,] solution, bool[,] given)
    {
        this.solution = solution;
        current = (bool[,])given.Clone();
    }

    static bool[,] Neighbor(bool[,] m, Random rng)
    {
        bool[,] n = (bool[,])m.Clone();
        int a, b;
        do { a = rng.Next(81); } while (!n[a / 9, a % 9]);
        do { b = rng.Next(81); } while (n[b / 9, b % 9]);
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

    public IEnumerator Run(Config cfg)
    {
        Finished = false; Stop = false;
        Iterations = 0; Accepted = 0;
        History.Clear();
        var rng = new Random(cfg.seed);

        int clues = CountTrue(current);
        SudokuFitness.Eval e0 = SudokuFitness.Evaluate(solution, current, cfg.targetDifficulty);
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
                SudokuFitness.Eval e = SudokuFitness.Evaluate(solution, nb, cfg.targetDifficulty);
                float delta = e.energy - CurrentEnergy;

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

                if (sw.ElapsedMilliseconds > 8) { yield return null; sw.Restart(); }
            }
            History.Add(BestEnergy);
            Temperature *= cfg.cooling;
        }
        Finished = true;
    }

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
