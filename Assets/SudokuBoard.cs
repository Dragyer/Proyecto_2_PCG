using System;

/// <summary>
/// Estado del Sudoku: grilla 9x9 (0 = vacía), máscara de pistas fijas
/// y máscara de candidatos por celda (bit d activo = el dígito d es posible).
/// </summary>
public class SudokuBoard
{
    public const int N = 9;
    public const int Box = 3;

    public int[,] Grid = new int[N, N];
    public bool[,] Given = new bool[N, N];
    public ushort[,] Candidates = new ushort[N, N];

    public SudokuBoard Clone()
    {
        var b = new SudokuBoard();
        Array.Copy(Grid, b.Grid, Grid.Length);
        Array.Copy(Given, b.Given, Given.Length);
        Array.Copy(Candidates, b.Candidates, Candidates.Length);
        return b;
    }

    public int CountGivens()
    {
        int n = 0;
        foreach (bool g in Given) if (g) n++;
        return n;
    }

    /// <summary>¿Se puede poner v en (r,c) sin repetir en fila, columna o bloque?</summary>
    public bool CanPlace(int r, int c, int v)
    {
        for (int i = 0; i < N; i++)
        {
            if (i != c && Grid[r, i] == v) return false;
            if (i != r && Grid[i, c] == v) return false;
        }
        int br = r / Box * Box, bc = c / Box * Box;
        for (int i = 0; i < Box; i++)
            for (int j = 0; j < Box; j++)
                if ((br + i != r || bc + j != c) && Grid[br + i, bc + j] == v) return false;
        return true;
    }

    /// <summary>Recalcula la máscara de candidatos de todas las celdas vacías.</summary>
    public void RecomputeCandidates()
    {
        for (int r = 0; r < N; r++)
            for (int c = 0; c < N; c++)
            {
                if (Grid[r, c] != 0) { Candidates[r, c] = 0; continue; }
                ushort mask = 0;
                for (int v = 1; v <= N; v++)
                    if (CanPlace(r, c, v)) mask |= (ushort)(1 << v);
                Candidates[r, c] = mask;
            }
    }

    /// <summary>Cantidad de conflictos (celdas llenas que repiten dígito con otra).</summary>
    public int CountConflicts()
    {
        int n = 0;
        for (int r = 0; r < N; r++)
            for (int c = 0; c < N; c++)
                if (Grid[r, c] != 0 && !CanPlace(r, c, Grid[r, c])) n++;
        return n;
    }

    public bool IsSolved()
    {
        foreach (int v in Grid) if (v == 0) return false;
        return CountConflicts() == 0;
    }
}
