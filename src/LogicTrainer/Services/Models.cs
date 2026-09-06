using LogicTrainer.Domain;
using LogicTrainer.Exercises;

namespace LogicTrainer.Services;

public sealed class AppSettings
{
    public Difficulty DefaultDifficulty { get; set; } = Difficulty.Easy;
    public int HighlightArgb { get; set; } = Color.CornflowerBlue.ToArgb();
}

public sealed class SessionResult
{
    public DateTime Date { get; set; }
    public Difficulty Difficulty { get; set; }
    public int Correct { get; set; }
    public int Total { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<ExerciseKind, TypeResult> ByType { get; set; } = [];
}

public sealed class TypeResult { public int Correct { get; set; } public int Total { get; set; } }

public sealed class KnowledgeSession
{
    private static readonly ExerciseKind[] Plan = [ExerciseKind.Calculation, ExerciseKind.Calculation, ExerciseKind.Calculation,
        ExerciseKind.TruthTable, ExerciseKind.TruthTable, ExerciseKind.TruthTable, ExerciseKind.Matching, ExerciseKind.Matching,
        ExerciseKind.Selection, ExerciseKind.Selection];
    private readonly List<ExerciseKind> _order;
    private readonly List<(ExerciseKind Kind, bool Correct)> _answers = [];
    private readonly DateTime _started = DateTime.Now;
    public Difficulty Difficulty { get; }
    public int Position => _answers.Count;
    public bool IsComplete => Position == 10;
    public ExerciseKind CurrentKind => _order[Position];
    public KnowledgeSession(Difficulty difficulty, Random? random = null)
    {
        Difficulty = difficulty; var r = random ?? Random.Shared; _order = Plan.OrderBy(_ => r.Next()).ToList();
    }
    public bool Record(Exercise exercise)
    {
        if (IsComplete || !exercise.IsSubmitted || exercise.Kind != CurrentKind) return false;
        _answers.Add((exercise.Kind, exercise.IsCorrect == true)); return true;
    }
    public SessionResult Finish()
    {
        if (!IsComplete) throw new InvalidOperationException("Сессия ещё не завершена.");
        return new SessionResult { Date = DateTime.Now, Difficulty = Difficulty, Correct = _answers.Count(x => x.Correct), Total = 10,
            Duration = DateTime.Now - _started, ByType = _answers.GroupBy(x => x.Kind).ToDictionary(g => g.Key,
                g => new TypeResult { Correct = g.Count(x => x.Correct), Total = g.Count() }) };
    }
}
