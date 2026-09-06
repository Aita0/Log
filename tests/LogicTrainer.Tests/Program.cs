using LogicTrainer.Domain;
using LogicTrainer.Exercises;
using LogicTrainer.Services;

var tests = new List<(string Name, Action Run)>
{
    ("Таблицы всех операций", TestOperations), ("Составное выражение", TestComposite),
    ("Все комбинации", TestCombinations), ("Пусто отличается от нуля", TestEmptyCell),
    ("Частичный набор отклоняется", TestPartialSelection), ("Пары однозначны", TestMatching),
    ("Ответ учитывается один раз", TestSingleSubmission), ("Сессия завершается после 10", TestSession)
};
var failures = 0;
foreach (var test in tests) { try { test.Run(); Console.WriteLine($"PASS: {test.Name}"); } catch (Exception e) { failures++; Console.WriteLine($"FAIL: {test.Name}: {e.Message}"); } }
return failures;

static void Assert(bool condition, string message = "Проверка не выполнена") { if (!condition) throw new Exception(message); }
static void TestOperations()
{
    var expected = new Dictionary<LogicOperation, bool[]> {
        [LogicOperation.And] = [false,false,false,true], [LogicOperation.Or] = [false,true,true,true],
        [LogicOperation.Xor] = [false,true,true,false], [LogicOperation.Nand] = [true,true,true,false],
        [LogicOperation.Nor] = [true,false,false,false], [LogicOperation.Equivalence] = [true,false,false,true],
        [LogicOperation.Implication] = [true,true,false,true] };
    Assert(LogicOperation.Not.Apply(false) && !LogicOperation.Not.Apply(true));
    var rows = new[] { (false,false), (false,true), (true,false), (true,true) };
    foreach (var item in expected) Assert(rows.Select(x => item.Key.Apply(x.Item1, x.Item2)).SequenceEqual(item.Value), item.Key.ToString());
}
static void TestComposite()
{
    var expression = new BinaryExpression(new BinaryExpression(new VariableExpression("A"), LogicOperation.And, new VariableExpression("B")), LogicOperation.Or, new VariableExpression("C"));
    Assert(expression.Evaluate(new Dictionary<string, bool>{{"A",true},{"B",false},{"C",true}}));
}
static void TestCombinations()
{
    var rows = TruthTable.Combinations(["A","B","C"]); Assert(rows.Count == 8); Assert(rows[0].Values.All(x => !x)); Assert(rows[7].Values.All(x => x));
}
static void TestEmptyCell()
{
    var e = new TruthTableExercise(new VariableExpression("A")); Assert(e.Answers[0] is null && !e.CanSubmit); e.SetAnswer(0, false); Assert(e.Answers[0] == false);
}
static void TestPartialSelection()
{
    Expression[] options = [new VariableExpression("A"), new UnaryExpression(LogicOperation.Not, new VariableExpression("A")), new VariableExpression("B"), new UnaryExpression(LogicOperation.Not, new VariableExpression("B"))];
    var e = new SelectionExercise(options, new(){{"A",true},{"B",true}}, true); e.Toggle(0); Assert(!e.Submit());
}
static void TestMatching()
{
    var ops = new[] { LogicOperation.And, LogicOperation.Or, LogicOperation.Xor }; var e = new MatchingExercise(ops, ops); e.Pair(0,0); e.Pair(1,0); Assert(e.Pairs.Count == 1); e.Pair(1,1); e.Pair(2,2); Assert(e.Submit());
}
static void TestSingleSubmission()
{
    var e = new CalculationExercise(new VariableExpression("A"), new(){{"A",true}}); e.SetAnswer(true); Assert(e.Submit()); Assert(!e.Submit());
}
static void TestSession()
{
    var session = new KnowledgeSession(Difficulty.Easy, new Random(1)); var factory = new ExerciseFactory(new Random(2));
    for (var i=0; i<10; i++) { var e = factory.Create(session.CurrentKind, Difficulty.Easy); SubmitAny(e); Assert(session.Record(e)); }
    Assert(session.IsComplete && session.Finish().Total == 10);
}
static void SubmitAny(Exercise e)
{
    switch (e) { case CalculationExercise x: x.SetAnswer(false); break; case TruthTableExercise x: for(var i=0;i<x.Answers.Length;i++) x.SetAnswer(i,false); break; case MatchingExercise x: for(var i=0;i<3;i++) x.Pair(i,i); break; case SelectionExercise x: x.Toggle(0); break; }
    e.Submit();
}
