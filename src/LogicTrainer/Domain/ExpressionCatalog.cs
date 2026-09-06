namespace LogicTrainer.Domain;

public enum Difficulty { Easy, Medium, Hard }

public static class ExpressionCatalog
{
    private static VariableExpression V(string name) => new(name);
    private static UnaryExpression N(Expression value) => new(LogicOperation.Not, value);
    private static BinaryExpression B(Expression a, LogicOperation op, Expression b) => new(a, op, b);

    public static IReadOnlyList<Expression> Get(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => [N(V("A")), B(V("A"), LogicOperation.And, V("B")), B(V("A"), LogicOperation.Or, V("B")), B(V("A"), LogicOperation.Xor, V("B")), B(V("A"), LogicOperation.Implication, V("B")), B(V("A"), LogicOperation.Equivalence, V("B")), B(V("A"), LogicOperation.Nand, V("B")), B(V("A"), LogicOperation.Nor, V("B"))],
        Difficulty.Medium => [B(B(V("A"), LogicOperation.And, V("B")), LogicOperation.Or, V("C")), N(B(V("A"), LogicOperation.Or, V("B"))), B(B(V("A"), LogicOperation.Xor, V("B")), LogicOperation.Implication, V("C")), B(B(V("A"), LogicOperation.Nand, V("B")), LogicOperation.Or, V("C")), B(V("A"), LogicOperation.Equivalence, N(V("B")))],
        Difficulty.Hard => [B(N(B(V("A"), LogicOperation.And, V("B"))), LogicOperation.Or, V("C")), B(B(V("A"), LogicOperation.Implication, V("B")), LogicOperation.And, B(V("B"), LogicOperation.Implication, V("C"))), N(B(B(V("A"), LogicOperation.Xor, V("B")), LogicOperation.Or, V("C"))), B(B(V("A"), LogicOperation.Nor, V("B")), LogicOperation.Equivalence, N(V("C")))],
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
    };
}

public static class TruthTable
{
    public static IReadOnlyList<Dictionary<string, bool>> Combinations(IEnumerable<string> variables)
    {
        var names = variables.Order().ToArray();
        return Enumerable.Range(0, 1 << names.Length).Select(number => names.Select((name, index) =>
            new { name, value = (number & (1 << (names.Length - index - 1))) != 0 })
            .ToDictionary(x => x.name, x => x.value)).ToArray();
    }
}
