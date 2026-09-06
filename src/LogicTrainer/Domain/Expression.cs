namespace LogicTrainer.Domain;

public abstract record Expression
{
    public abstract bool Evaluate(IReadOnlyDictionary<string, bool> values);
    public abstract IReadOnlySet<string> Variables { get; }
    public abstract string Explain(IReadOnlyDictionary<string, bool> values);
    protected static int Bit(bool value) => value ? 1 : 0;
}

public sealed record VariableExpression(string Name) : Expression
{
    public override bool Evaluate(IReadOnlyDictionary<string, bool> values) => values[Name];
    public override IReadOnlySet<string> Variables => new HashSet<string> { Name };
    public override string ToString() => Name;
    public override string Explain(IReadOnlyDictionary<string, bool> values) => $"{Name} = {Bit(Evaluate(values))}";
}

public sealed record UnaryExpression(LogicOperation Operation, Expression Operand) : Expression
{
    public override bool Evaluate(IReadOnlyDictionary<string, bool> values) => Operation.Apply(Operand.Evaluate(values));
    public override IReadOnlySet<string> Variables => Operand.Variables;
    public override string ToString() => $"{Operation.Symbol()}({Operand})";
    public override string Explain(IReadOnlyDictionary<string, bool> values) =>
        $"{Operand.Explain(values)}; {this} = {Bit(Evaluate(values))}";
}

public sealed record BinaryExpression(Expression Left, LogicOperation Operation, Expression Right) : Expression
{
    public override bool Evaluate(IReadOnlyDictionary<string, bool> values) =>
        Operation.Apply(Left.Evaluate(values), Right.Evaluate(values));
    public override IReadOnlySet<string> Variables => new HashSet<string>(Left.Variables.Concat(Right.Variables));
    public override string ToString() => $"({Left} {Operation.Symbol()} {Right})";
    public override string Explain(IReadOnlyDictionary<string, bool> values)
    {
        var left = Left.Evaluate(values); var right = Right.Evaluate(values);
        return $"{Left} = {Bit(left)}; {Right} = {Bit(right)}; {this} = {Bit(Evaluate(values))}.";
    }
}
