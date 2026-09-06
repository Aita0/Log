using LogicTrainer.Domain;

namespace LogicTrainer.Exercises;

public enum ExerciseKind { Calculation, TruthTable, Matching, Selection }

public abstract class Exercise
{
    public abstract ExerciseKind Kind { get; }
    public bool IsSubmitted { get; private set; }
    public bool? IsCorrect { get; private set; }
    public event EventHandler? AnswerChanged;
    public abstract bool CanSubmit { get; }
    public abstract string Explanation { get; }
    protected void Changed() => AnswerChanged?.Invoke(this, EventArgs.Empty);
    protected abstract bool CheckCore();
    public bool Submit()
    {
        if (IsSubmitted || !CanSubmit) return false;
        IsCorrect = CheckCore(); IsSubmitted = true; Changed(); return IsCorrect.Value;
    }
    public void Retry()
    {
        IsSubmitted = false; IsCorrect = null; Clear(); Changed();
    }
    public abstract void Clear();
}

public sealed class CalculationExercise(Expression expression, Dictionary<string, bool> values) : Exercise
{
    public override ExerciseKind Kind => ExerciseKind.Calculation;
    public Expression Expression { get; } = expression;
    public Dictionary<string, bool> Values { get; } = values;
    public bool? Answer { get; private set; }
    public void SetAnswer(bool value) { if (!IsSubmitted) { Answer = value; Changed(); } }
    public override bool CanSubmit => Answer.HasValue && !IsSubmitted;
    protected override bool CheckCore() => Answer == Expression.Evaluate(Values);
    public override string Explanation => Expression.Explain(Values);
    public override void Clear() { if (!IsSubmitted) { Answer = null; Changed(); } }
}

public sealed class TruthTableExercise : Exercise
{
    public override ExerciseKind Kind => ExerciseKind.TruthTable;
    public Expression Expression { get; }
    public IReadOnlyList<Dictionary<string, bool>> Rows { get; }
    public bool?[] Answers { get; }
    public TruthTableExercise(Expression expression) { Expression = expression; Rows = TruthTable.Combinations(expression.Variables); Answers = new bool?[Rows.Count]; }
    public void SetAnswer(int row, bool? value) { if (!IsSubmitted) { Answers[row] = value; Changed(); } }
    public override bool CanSubmit => !IsSubmitted && Answers.All(x => x.HasValue);
    protected override bool CheckCore() => Rows.Select((r, i) => Answers[i] == Expression.Evaluate(r)).All(x => x);
    public IEnumerable<int> IncorrectRows => Rows.Select((r, i) => (r, i)).Where(x => Answers[x.i] != Expression.Evaluate(x.r)).Select(x => x.i);
    public override string Explanation => string.Join(Environment.NewLine, Rows.Select((r, i) => $"Строка {i + 1}: ожидается {(Expression.Evaluate(r) ? 1 : 0)}"));
    public override void Clear() { if (!IsSubmitted) { Array.Fill(Answers, null); Changed(); } }
}

public sealed class MatchingExercise : Exercise
{
    public override ExerciseKind Kind => ExerciseKind.Matching;
    public IReadOnlyList<LogicOperation> Operations { get; }
    public IReadOnlyList<LogicOperation> Tables { get; }
    public Dictionary<int, int> Pairs { get; } = [];
    public MatchingExercise(IReadOnlyList<LogicOperation> operations, IReadOnlyList<LogicOperation> tables) { Operations = operations; Tables = tables; }
    public void Pair(int operationIndex, int tableIndex)
    {
        if (IsSubmitted || Pairs.ContainsKey(operationIndex) || Pairs.ContainsValue(tableIndex)) return;
        Pairs[operationIndex] = tableIndex; Changed();
    }
    public void Unpair(int operationIndex) { if (!IsSubmitted && Pairs.Remove(operationIndex)) Changed(); }
    public override bool CanSubmit => !IsSubmitted && Pairs.Count == 3;
    protected override bool CheckCore() => Pairs.All(p => Operations[p.Key] == Tables[p.Value]);
    public override string Explanation => string.Join(Environment.NewLine, Operations.Select(x => $"{x.RussianName()}: {x.Rule()}."));
    public override void Clear() { if (!IsSubmitted) { Pairs.Clear(); Changed(); } }
}

public sealed class SelectionExercise(IReadOnlyList<Expression> options, Dictionary<string, bool> values, bool target) : Exercise
{
    public override ExerciseKind Kind => ExerciseKind.Selection;
    public IReadOnlyList<Expression> Options { get; } = options;
    public Dictionary<string, bool> Values { get; } = values;
    public bool Target { get; } = target;
    public HashSet<int> Selected { get; } = [];
    public IReadOnlySet<int> Correct => Options.Select((x, i) => (x, i)).Where(x => x.x.Evaluate(Values) == Target).Select(x => x.i).ToHashSet();
    public void Toggle(int index) { if (!IsSubmitted) { if (!Selected.Add(index)) Selected.Remove(index); Changed(); } }
    public override bool CanSubmit => !IsSubmitted && Selected.Count > 0;
    protected override bool CheckCore() => Selected.SetEquals(Correct);
    public override string Explanation => string.Join(Environment.NewLine, Options.Select(x => $"{x} = {(x.Evaluate(Values) ? 1 : 0)}"));
    public override void Clear() { if (!IsSubmitted) { Selected.Clear(); Changed(); } }
}
