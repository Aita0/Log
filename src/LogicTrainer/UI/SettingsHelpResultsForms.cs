using LogicTrainer.Domain;
using LogicTrainer.Exercises;
using LogicTrainer.Services;

namespace LogicTrainer.UI;

public sealed class SettingsForm : Form
{
    public SettingsForm(AppSettings settings, JsonStorage storage)
    {
        Text = "Настройки"; Size = new Size(520, 300); StartPosition = FormStartPosition.CenterScreen;
        var difficulty = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        difficulty.Items.AddRange(new object[] { "Лёгкий", "Средний", "Сложный" }); difficulty.SelectedIndex = (int)settings.DefaultDifficulty;
        var color = new Button { Text = "Выбрать цвет", BackColor = Color.FromArgb(settings.HighlightArgb), AutoSize = true };
        color.Click += (_, _) => { using var dialog = new ColorDialog { Color = color.BackColor }; if (dialog.ShowDialog() == DialogResult.OK) color.BackColor = dialog.Color; };
        var save = UiHelpers.Button("Сохранить", (_, _) => { settings.DefaultDifficulty = (Difficulty)difficulty.SelectedIndex; settings.HighlightArgb = color.BackColor.ToArgb(); if (!storage.SaveSettings(settings, out var e)) MessageBox.Show($"Не удалось сохранить настройки: {e}"); else Close(); });
        var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(25) };
        p.Controls.Add(UiHelpers.Heading("Настройки")); p.Controls.Add(new Label { Text = "Сложность по умолчанию", AutoSize = true }); p.Controls.Add(difficulty); p.Controls.Add(new Label { Text = "Цвет выделения ответов", AutoSize = true }); p.Controls.Add(color); p.Controls.Add(save); Controls.Add(p);
    }
}

public sealed class HelpForm : Form
{
    public HelpForm()
    {
        Text = "Справка по операциям"; Size = new Size(900, 700); StartPosition = FormStartPosition.CenterScreen;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        foreach (var op in Enum.GetValues<LogicOperation>())
        {
            var page = new TabPage(op.RussianName()); var box = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Segoe UI Symbol", 12), BackColor = Color.White };
            var lines = new List<string> { $"{op.RussianName()}   Обозначение: {op.Symbol()}", "", $"Операция {op.Rule()}.", "", op == LogicOperation.Not ? "A | Результат" : "A B | Результат" };
            var rows = op == LogicOperation.Not ? TruthTable.Combinations(new[] { "A" }) : TruthTable.Combinations(new[] { "A", "B" });
            lines.AddRange(rows.Select(r => op == LogicOperation.Not ? $"{B(r["A"])} | {B(op.Apply(r["A"]))}" : $"{B(r["A"])} {B(r["B"])} | {B(op.Apply(r["A"], r["B"]))}"));
            box.Text = string.Join(Environment.NewLine, lines); page.Controls.Add(box); tabs.TabPages.Add(page);
        }
        Controls.Add(tabs);
    }
    private static int B(bool x) => x ? 1 : 0;
}

public sealed class ResultsForm : Form
{
    public ResultsForm(JsonStorage storage)
    {
        Text = "Результаты"; Size = new Size(950, 500); StartPosition = FormStartPosition.CenterScreen;
        var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false };
        grid.DataSource = storage.LoadResults().OrderByDescending(x => x.Date).Select(x => new { Дата = x.Date, Сложность = UiHelpers.DifficultyName(x.Difficulty), Правильно = $"{x.Correct} из {x.Total}", Процент = $"{x.Correct * 100 / x.Total}%", Длительность = x.Duration.ToString(@"mm\:ss"), По_типам = string.Join("; ", x.ByType.Select(p => $"{UiHelpers.KindName(p.Key)} {p.Value.Correct}/{p.Value.Total}")) }).ToList();
        Controls.Add(grid);
    }
}
