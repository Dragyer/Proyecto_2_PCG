using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Técnica evolutiva: Algoritmo Genético sobre las pistas de un Sudoku.
///
/// Individuo:   máscara de pistas (bool[9,9]) sobre una solución fija; siempre con K pistas.
/// Población:   el puzzle generado por el sistema (Backward from Goal State) y variantes
///              obtenidas con algunos intercambios de pistas.
/// Selección:   torneo de 3 individuos.
/// Cruce:       uniforme (cada celda viene de un padre) + reparación para volver a K pistas.
/// Mutación:    con probabilidad mutationRate, 1 a 3 intercambios (pista por celda oculta).
/// Elitismo:    el 10% mejor pasa intacto a la siguiente generación.
/// Evaluación:  SudokuFitness (compartida con el recocido simulado).
/// </summary>
public class SudokuEvolver
{
    public struct Config
    {
        public int targetDifficulty;
        public int populationSize;
        public int generations;
        public float mutationRate;
        public int seed;
    }

    class Individual
    {
        public bool[,] mask;
        public float energy;
        public int solutions, difficulty;
    }

    readonly int[,] solution;
    readonly bool[,] seedMask;
    int clues;

    public bool Stop, Finished;
    public int Generation, Evaluations;
    public float StartEnergy, BestEnergy, MeanEnergy;
    public int StartDifficulty, StartSolutions, BestDifficulty, BestSolutions;
    public List<float> History = new List<float>();

    Individual best;

    public SudokuEvolver(int[,] solution, bool[,] given)
    {
        this.solution = solution;
        seedMask = (bool[,])given.Clone();
    }

    static int CountTrue(bool[,] m)
    {
        int n = 0;
        foreach (bool v in m) if (v) n++;
        return n;
    }

    static void Swap(bool[,] m, Random rng)
    {
        int a, b;
        do { a = rng.Next(81); } while (!m[a / 9, a % 9]);
        do { b = rng.Next(81); } while (m[b / 9, b % 9]);
        m[a / 9, a % 9] = false;
        m[b / 9, b % 9] = true;
    }

    /// <summary>Deja la máscara con exactamente 'clues' pistas.</summary>
    void Repair(bool[,] m, Random rng)
    {
        int n = CountTrue(m);
        while (n > clues)
        {
            int i; do { i = rng.Next(81); } while (!m[i / 9, i % 9]);
            m[i / 9, i % 9] = false; n--;
        }
        while (n < clues)
        {
            int i; do { i = rng.Next(81); } while (m[i / 9, i % 9]);
            m[i / 9, i % 9] = true; n++;
        }
    }

    Individual Tournament(List<Individual> pop, Random rng)
    {
        Individual winner = pop[rng.Next(pop.Count)];
        for (int i = 1; i < 3; i++)
        {
            Individual c = pop[rng.Next(pop.Count)];
            if (c.energy < winner.energy) winner = c;
        }
        return winner;
    }

    bool[,] Crossover(Individual a, Individual b, Random rng)
    {
        bool[,] child = new bool[9, 9];
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                child[r, c] = rng.Next(2) == 0 ? a.mask[r, c] : b.mask[r, c];
        Repair(child, rng);
        return child;
    }

    void Score(Individual ind, int target)
    {
        SudokuFitness.Eval e = SudokuFitness.Evaluate(solution, ind.mask, target);
        ind.energy = e.energy; ind.solutions = e.solutions; ind.difficulty = e.difficulty;
        Evaluations++;
    }

    void UpdateStats(List<Individual> pop)
    {
        float sum = 0f;
        Individual b = pop[0];
        foreach (Individual i in pop) { sum += i.energy; if (i.energy < b.energy) b = i; }
        best = b;
        BestEnergy = b.energy; BestDifficulty = b.difficulty; BestSolutions = b.solutions;
        MeanEnergy = sum / pop.Count;
    }

    public IEnumerator Run(Config cfg)
    {
        Finished = false; Stop = false;
        Generation = 0; Evaluations = 0;
        History.Clear();
        var rng = new Random(cfg.seed);
        clues = CountTrue(seedMask);
        if (clues == 0 || clues == 81) { Finished = true; yield break; }

        var sw = Stopwatch.StartNew();
        int size = Math.Max(4, cfg.populationSize);

        // población inicial: el puzzle original + variantes con 3 a 10 intercambios
        var pop = new List<Individual>();
        for (int i = 0; i < size; i++)
        {
            bool[,] m = (bool[,])seedMask.Clone();
            if (i > 0)
            {
                int swaps = 3 + rng.Next(8);
                for (int s = 0; s < swaps; s++) Swap(m, rng);
            }
            var ind = new Individual { mask = m };
            Score(ind, cfg.targetDifficulty);
            pop.Add(ind);
            if (sw.ElapsedMilliseconds > 8) { yield return null; sw.Restart(); }
        }
        StartEnergy = pop[0].energy; StartDifficulty = pop[0].difficulty; StartSolutions = pop[0].solutions;
        UpdateStats(pop);
        History.Add(BestEnergy);

        int elites = Math.Max(1, size / 10);
        for (Generation = 1; Generation <= cfg.generations && !Stop && BestEnergy > 0f; Generation++)
        {
            pop.Sort((x, y) => x.energy.CompareTo(y.energy));
            var next = new List<Individual>();
            for (int i = 0; i < elites; i++) next.Add(pop[i]); // elitismo

            while (next.Count < size && !Stop)
            {
                Individual p1 = Tournament(pop, rng);
                Individual p2 = Tournament(pop, rng);
                bool[,] childMask = rng.NextDouble() < 0.8 ? Crossover(p1, p2, rng) : (bool[,])p1.mask.Clone();
                if (rng.NextDouble() < cfg.mutationRate)
                {
                    int swaps = 1 + rng.Next(3);
                    for (int s = 0; s < swaps; s++) Swap(childMask, rng);
                }
                var child = new Individual { mask = childMask };
                Score(child, cfg.targetDifficulty);
                next.Add(child);
                if (sw.ElapsedMilliseconds > 8) { yield return null; sw.Restart(); }
            }
            if (next.Count < size) break; // detenido a mitad de generación: se conserva la población anterior
            pop = next;
            UpdateStats(pop);
            History.Add(BestEnergy);
        }
        Finished = true;
    }

    public SudokuBoard BuildBest()
    {
        var b = new SudokuBoard();
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                b.Given[r, c] = best.mask[r, c];
                b.Grid[r, c] = best.mask[r, c] ? solution[r, c] : 0;
            }
        b.RecomputeCandidates();
        return b;
    }
}
