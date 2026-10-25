using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Slider con nombre, rango y etiqueta con su valor actual.</summary>
[System.Serializable]
public class ParamSlider
{
    public Slider slider;
    public TMP_Text label;
    public string name;
    public float min, max, value;
    public bool whole;

    public float Value => slider.value;

    public void Init()
    {
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = whole;
        slider.value = value;
        slider.onValueChanged.AddListener(_ => Refresh());
        Refresh();
    }

    void Refresh()
    {
        if (label != null)
            label.text = name + ": " + slider.value.ToString(whole ? "0" : "0.##");
    }
}

public class SudokuUI : MonoBehaviour
{
    [Header("Grilla")]
    [SerializeField] Transform gridParent;
    [SerializeField] GameObject cellPrefab;

    [Header("Generación")]
    [SerializeField] Slider cluesSlider;
    [SerializeField] Button generateButton;
    [SerializeField] Toggle showSolutionToggle;      // opcional

    [Header("Objetivo común (GA y SA)")]
    [SerializeField] ParamSlider targetDifficulty = new ParamSlider { name = "Dificultad objetivo", min = 0, max = 200, value = 20, whole = true };

    [Header("Algoritmo genético")]
    [SerializeField] ParamSlider gaPopulation  = new ParamSlider { name = "Población", min = 6, max = 40, value = 16, whole = true };
    [SerializeField] ParamSlider gaGenerations = new ParamSlider { name = "Generaciones", min = 5, max = 100, value = 30, whole = true };
    [SerializeField] ParamSlider gaMutation    = new ParamSlider { name = "Tasa de mutación", min = 0.05f, max = 1f, value = 0.4f, whole = false };

    [Header("Simulated Annealing")]
    [SerializeField] ParamSlider initialTemp = new ParamSlider { name = "Temp. inicial", min = 1, max = 50, value = 10, whole = false };
    [SerializeField] ParamSlider cooling     = new ParamSlider { name = "Enfriamiento", min = 0.80f, max = 0.99f, value = 0.95f, whole = false };
    [SerializeField] ParamSlider minTemp     = new ParamSlider { name = "Temp. mínima", min = 0.01f, max = 1f, value = 0.1f, whole = false };
    [SerializeField] ParamSlider neighbors   = new ParamSlider { name = "Vecinos por temp.", min = 1, max = 30, value = 10, whole = true };

    [Header("Botones")]
    [SerializeField] Button evolveButton;            // solo GA
    [SerializeField] Button annealButton;            // solo SA
    [SerializeField] Button pipelineButton;          // GA y luego SA
    [SerializeField] Button stopButton;
    [SerializeField] Button restoreButton;           // vuelve al puzzle inicial

    [Header("Textos")]
    [SerializeField] TMP_Text infoText;
    [SerializeField] TMP_Text progressText;
    [SerializeField] TMP_Text logText;               // opcional

    TMP_Text[,] cells = new TMP_Text[9, 9];
    SudokuGenerator.Result current;
    SudokuBoard initialPuzzle;
    int initialDifficulty, puzzleSeed, runCount;
    SudokuEvolver evolver;
    SudokuAnnealer annealer;
    bool running, stopAll;
    readonly StringBuilder log = new StringBuilder();

    void Start()
    {
        cluesSlider.wholeNumbers = true;
        cluesSlider.minValue = SudokuGenerator.MinClues;
        cluesSlider.maxValue = 60;
        cluesSlider.value = 30;

        targetDifficulty.Init();
        gaPopulation.Init(); gaGenerations.Init(); gaMutation.Init();
        initialTemp.Init(); cooling.Init(); minTemp.Init(); neighbors.Init();

        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                GameObject go = Instantiate(cellPrefab, gridParent);
                TMP_Text t = go.GetComponentInChildren<TMP_Text>();
                cells[r, c] = t;

                RectTransform rt = t.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                t.alignment = TextAlignmentOptions.Center;
                t.margin = Vector4.zero;

                bool dark = ((r / 3) + (c / 3)) % 2 == 0;
                go.GetComponent<Image>().color = dark ? new Color(0.82f, 0.87f, 0.96f) : Color.white;
            }

        generateButton.onClick.AddListener(Generate);
        evolveButton.onClick.AddListener(() => StartCoroutine(Wrap(GACo())));
        annealButton.onClick.AddListener(() => StartCoroutine(Wrap(SACo())));
        pipelineButton.onClick.AddListener(() => StartCoroutine(Wrap(Pipeline())));
        stopButton.onClick.AddListener(StopAll);
        restoreButton.onClick.AddListener(RestoreInitial);
        if (showSolutionToggle != null)
            showSolutionToggle.onValueChanged.AddListener(_ => Redraw());

        SetRunning(false);
        Generate();
    }

    void Generate()
    {
        puzzleSeed = Random.Range(0, int.MaxValue);
        current = SudokuGenerator.Generate((int)cluesSlider.value, puzzleSeed);
        initialPuzzle = current.Puzzle.Clone();
        initialDifficulty = current.Difficulty;
        runCount = 0;
        log.Clear();
        if (logText != null) logText.text = "";
        if (progressText != null) progressText.text = "";
        Redraw();
        UpdateInfo();
    }

    void RestoreInitial()
    {
        if (running) return;
        current.Puzzle = initialPuzzle.Clone();
        current.Difficulty = initialDifficulty;
        Redraw();
        UpdateInfo();
    }

    void StopAll()
    {
        stopAll = true;
        if (evolver != null) evolver.Stop = true;
        if (annealer != null) annealer.Stop = true;
    }

    IEnumerator Wrap(IEnumerator inner)
    {
        if (running) yield break;
        stopAll = false;
        SetRunning(true);
        yield return StartCoroutine(inner);
        SetRunning(false);
    }

    IEnumerator Pipeline()
    {
        yield return StartCoroutine(GACo());
        if (!stopAll) yield return StartCoroutine(SACo());
    }

    // ---------- Algoritmo genético ----------
    IEnumerator GACo()
    {
        var cfg = new SudokuEvolver.Config
        {
            targetDifficulty = (int)targetDifficulty.Value,
            populationSize = (int)gaPopulation.Value,
            generations = (int)gaGenerations.Value,
            mutationRate = gaMutation.Value,
            seed = puzzleSeed
        };
        evolver = new SudokuEvolver(current.Solution, current.Puzzle.Given);
        IEnumerator it = evolver.Run(cfg);
        var e = evolver;
        while (it.MoveNext())
        {
            progressText.text = $"GA | generación {e.Generation}/{cfg.generations} | evaluaciones {e.Evaluations}\n" +
                                $"mejor energía: {e.BestEnergy:0.#} | promedio: {e.MeanEnergy:0.#}";
            yield return null;
        }

        current.Puzzle = e.BuildBest();
        current.Difficulty = e.BestDifficulty;
        Redraw();
        UpdateInfo();

        runCount++;
        AddLog($"#{runCount} GA obj={cfg.targetDifficulty} pob={cfg.populationSize} gen={cfg.generations} mut={cfg.mutationRate:0.##} | " +
               $"dificultad {e.StartDifficulty}->{e.BestDifficulty} | soluciones {e.StartSolutions}->{e.BestSolutions} | " +
               $"energía {e.StartEnergy:0.#}->{e.BestEnergy:0.#} | evaluaciones {e.Evaluations}");
        progressText.text = (stopAll ? "GA detenido. " : "GA terminado. ") + $"Mejor energía: {e.BestEnergy:0.#}";
    }

    // ---------- Simulated Annealing ----------
    IEnumerator SACo()
    {
        var cfg = new SudokuAnnealer.Config
        {
            targetDifficulty = (int)targetDifficulty.Value,
            initialTemp = initialTemp.Value,
            cooling = cooling.Value,
            minTemp = minTemp.Value,
            neighborsPerTemp = (int)neighbors.Value,
            seed = puzzleSeed
        };
        annealer = new SudokuAnnealer(current.Solution, current.Puzzle.Given);
        IEnumerator it = annealer.Run(cfg);
        var a = annealer;
        while (it.MoveNext())
        {
            progressText.text = $"SA | T = {a.Temperature:0.000} | iteraciones {a.Iterations} | aceptados {a.Accepted}\n" +
                                $"energía actual: {a.CurrentEnergy:0.#} | mejor: {a.BestEnergy:0.#}";
            yield return null;
        }

        current.Puzzle = a.BuildBest();
        current.Difficulty = a.BestDifficulty;
        Redraw();
        UpdateInfo();

        runCount++;
        AddLog($"#{runCount} SA obj={cfg.targetDifficulty} T0={cfg.initialTemp:0.##} enf={cfg.cooling:0.00} Tmin={cfg.minTemp:0.##} vec={cfg.neighborsPerTemp} | " +
               $"dificultad {a.StartDifficulty}->{a.BestDifficulty} | soluciones {a.StartSolutions}->{a.BestSolutions} | " +
               $"energía {a.StartEnergy:0.#}->{a.BestEnergy:0.#} | iteraciones {a.Iterations}");
        progressText.text = (stopAll ? "SA detenido. " : "SA terminado. ") + $"Mejor energía: {a.BestEnergy:0.#}";
    }

    void AddLog(string line)
    {
        log.AppendLine(line);
        if (logText != null) logText.text = log.ToString();
    }

    void SetRunning(bool r)
    {
        running = r;
        generateButton.interactable = !r;
        evolveButton.interactable = !r;
        annealButton.interactable = !r;
        pipelineButton.interactable = !r;
        restoreButton.interactable = !r;
        stopButton.interactable = r;
    }

    void UpdateInfo()
    {
        infoText.text = $"Pistas: {current.Puzzle.CountGivens()} | Dificultad (heurística): {current.Difficulty} | Semilla: {puzzleSeed}";
    }

    void Redraw()
    {
        if (current == null) return;
        bool showSol = showSolutionToggle != null && showSolutionToggle.isOn;
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                bool given = current.Puzzle.Given[r, c];
                int v = given || showSol ? current.Solution[r, c] : 0;
                cells[r, c].text = v == 0 ? "" : v.ToString();
                cells[r, c].color = given ? Color.black : new Color(0.1f, 0.4f, 0.9f);
            }
    }
}
