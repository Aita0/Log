using LogicTrainer.Domain;

namespace LogicTrainer.Exercises;

public sealed class ExerciseFactory(Random? random = null)
{
    private readonly Random _random = random ?? Random.Shared;
    private string? _lastExpression;
    private Expression Pick(Difficulty difficulty)
    {
        var available = ExpressionCatalog.Get(difficulty).Where(x => x.ToString() != _lastExpression).ToArray();
        var result = available[_random.Next(available.Length)]; _lastExpression = result.ToString(); return result;
    }
    private Dictionary<string, bool> Values(IEnumerable<string> names) => names.ToDictionary(x => x, _ => _random.Next(2) == 1);
    public Exercise Create(ExerciseKind kind, Difficulty difficulty) => kind switch
    {
        ExerciseKind.Calculation => Calculation(difficulty), ExerciseKind.TruthTable => new TruthTableExercise(Pick(difficulty)),
        ExerciseKind.Matching => Matching(), ExerciseKind.Selection => Selection(difficulty), _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private CalculationExercise Calculation(Difficulty d) { var e = Pick(d); return new(e, Values(e.Variables)); }
    private MatchingExercise Matching()
    {
        var operations = LogicOperations.Binary.OrderBy(_ => _random.Next()).Take(3).ToArray();
        return new(operations, operations.OrderBy(_ => _random.Next()).ToArray());
    }
    private SelectionExercise Selection(Difficulty d)
    {
        var pool = ExpressionCatalog.Get(d).Concat(ExpressionCatalog.Get(Difficulty.Easy)).Distinct().ToArray();
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var options = pool.OrderBy(_ => _random.Next()).Take(4).ToArray();
            var values = Values(options.SelectMany(x => x.Variables).Distinct()); var target = _random.Next(2) == 1;
            var result = new SelectionExercise(options, values, target);
            if (result.Correct.Count is >= 1 and <= 3) return result;
        }
        throw new InvalidOperationException("Не удалось сформировать варианты.");
    }
}
