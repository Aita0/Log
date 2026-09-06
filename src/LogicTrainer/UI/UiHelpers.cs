namespace LogicTrainer.UI;

internal static class UiHelpers
{
    public static Label Heading(string text, float size = 20) => new() { Text = text, AutoSize = true, Font = new Font("Segoe UI", size, FontStyle.Bold), Margin = new Padding(8) };
    public static Button Button(string text, EventHandler action) { var b = new Button { Text = text, AutoSize = true, Padding = new Padding(12, 6, 12, 6), Margin = new Padding(6) }; b.Click += action; return b; }
    public static string DifficultyName(Domain.Difficulty d) => d switch { Domain.Difficulty.Easy => "Лёгкий", Domain.Difficulty.Medium => "Средний", _ => "Сложный" };
    public static string KindName(Exercises.ExerciseKind k) => k switch { Exercises.ExerciseKind.Calculation => "Вычисление", Exercises.ExerciseKind.TruthTable => "Таблица", Exercises.ExerciseKind.Matching => "Пары", _ => "Выбор выражений" };
}
