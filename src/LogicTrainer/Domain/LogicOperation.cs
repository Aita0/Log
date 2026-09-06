namespace LogicTrainer.Domain;

public enum LogicOperation { Not, And, Or, Xor, Nand, Nor, Equivalence, Implication }

public static class LogicOperations
{
    public static readonly LogicOperation[] Binary = Enum.GetValues<LogicOperation>()
        .Where(x => x != LogicOperation.Not).ToArray();

    public static string Symbol(this LogicOperation operation) => operation switch
    {
        LogicOperation.Not => "¬", LogicOperation.And => "∧", LogicOperation.Or => "∨",
        LogicOperation.Xor => "⊕", LogicOperation.Nand => "↑", LogicOperation.Nor => "↓",
        LogicOperation.Equivalence => "↔", LogicOperation.Implication => "→", _ => "?"
    };

    public static string RussianName(this LogicOperation operation) => operation switch
    {
        LogicOperation.Not => "Отрицание (NOT)", LogicOperation.And => "Конъюнкция (AND)",
        LogicOperation.Or => "Дизъюнкция (OR)", LogicOperation.Xor => "Исключающее ИЛИ (XOR)",
        LogicOperation.Nand => "Отрицание конъюнкции (NAND)",
        LogicOperation.Nor => "Отрицание дизъюнкции (NOR)",
        LogicOperation.Equivalence => "Эквивалентность", LogicOperation.Implication => "Импликация", _ => ""
    };

    public static string Rule(this LogicOperation operation) => operation switch
    {
        LogicOperation.Not => "меняет 0 на 1, а 1 на 0",
        LogicOperation.And => "истинна, когда оба аргумента равны 1",
        LogicOperation.Or => "истинна, когда хотя бы один аргумент равен 1",
        LogicOperation.Xor => "истинна, когда аргументы различны",
        LogicOperation.Nand => "ложна только когда оба аргумента равны 1",
        LogicOperation.Nor => "истинна только когда оба аргумента равны 0",
        LogicOperation.Equivalence => "истинна, когда аргументы одинаковы",
        LogicOperation.Implication => "ложна только при A = 1 и B = 0",
        _ => ""
    };

    public static bool Apply(this LogicOperation operation, bool a, bool b = false) => operation switch
    {
        LogicOperation.Not => !a, LogicOperation.And => a && b, LogicOperation.Or => a || b,
        LogicOperation.Xor => a ^ b, LogicOperation.Nand => !(a && b), LogicOperation.Nor => !(a || b),
        LogicOperation.Equivalence => a == b, LogicOperation.Implication => !a || b,
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}
