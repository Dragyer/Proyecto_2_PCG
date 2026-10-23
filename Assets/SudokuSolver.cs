using System;

/// <summary>
/// Solver por backtracking con la heurística MRV (Minimum Remaining Values):
/// en cada paso se expande la celda vacía con menos candidatos.
/// Sirve para (a) generar una solución completa aleatoria y
/// (b) contar soluciones (cortando en 'limit') para garantizar unicidad.
/// </summary>
public class SudokuSolver
{
    const int N = SudokuBoard.N;

    readonly int[,] g = new int[N, N];
    readonly int[] rowM = new int[N], colM = new int[N], boxM = new int[N];
    Random rng;
    int limit, count;

    public int Nodes { get; private set; }
    public int[,] Solution { get; private set; }

    public SudokuSolver(int[,] grid)
    {
        for (int r = 0; r < N; r++)
            for (int c = 0; c < N; c++)
                if (grid[r, c] != 0) Set(r, c, grid[r, c]);
    }

    static int BoxIndex(int r, int c) => r / 3 * 3 + c / 3;

    void Set(int r, int c, int v)
    {
        g[r, c] = v;
        int bit = 1 << v;
        rowM[r] |= bit; colM[c] |= bit; boxM[BoxIndex(r, c)] |= bit;
    }

    void Unset(int r, int c, int v)
    {
        g[r, c] = 0;
        int bit = ~(1 << v);
        rowM[r] &= bit; colM[c] &= bit; boxM[BoxIndex(r, c)] &= bit;
    }

    static int PopCount(int x)
    {
        int n = 0;
        while (x != 0) { x &= x - 1; n++; }
        return n;
    }

    /// <summary>Cuenta soluciones hasta 'limit'. Con rng != null el orden de dígitos es aleatorio.</summary>
    public int CountSolutions(int limit, Random rng = null)
    {
        this.limit = limit; this.rng = rng;
        count = 0; Nodes = 0; Solution = null;
        Search();
        return count;
    }

    void Search()
    {
        if (count >= limit) return;
        Nodes++;

        // Heurística MRV: elegir la celda vacía con menos candidatos
        int bestR = -1, bestC = -1, bestMask = 0, bestCount = 10;
        for (int r = 0; r < N; r++)
            for (int c = 0; c < N; c++)
            {
                if (g[r, c] != 0) continue;
                int mask = ~(rowM[r] | colM[c] | boxM[BoxIndex(r, c)]) & 0x3FE;
                int n = PopCount(mask);
                if (n == 0) return; // callejón sin salida
                if (n < bestCount) { bestCount = n; bestR = r; bestC = c; bestMask = mask; }
            }

        if (bestR == -1) // sin celdas vacías: solución encontrada
        {
            count++;
            if (count == 1) Solution = (int[,])g.Clone();
            return;
        }

        int[] digits = new int[bestCount];
        int k = 0;
        for (int v = 1; v <= N; v++)
            if ((bestMask & (1 << v)) != 0) digits[k++] = v;

        if (rng != null) // barajar para obtener variedad
            for (int i = digits.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (digits[i], digits[j]) = (digits[j], digits[i]);
            }

        foreach (int v in digits)
        {
            Set(bestR, bestC, v);
            Search();
            Unset(bestR, bestC, v);
            if (count >= limit) return;
        }
    }
}
