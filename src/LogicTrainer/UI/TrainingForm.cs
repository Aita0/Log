using LogicTrainer.Domain;
using LogicTrainer.Exercises;
using LogicTrainer.Services;

namespace LogicTrainer.UI;

public sealed class TrainingForm : Form
{
    private readonly AppSettings _settings; private readonly bool _exam; private readonly JsonStorage _storage;
    private readonly ExerciseFactory _factory = new(); private KnowledgeSession? _session; private Exercise? _exercise;
    private Difficulty _difficulty; private ExerciseKind _kind; private readonly Panel _content = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
    private readonly Label _status = new() { AutoSize = true, Padding = new Padding(6) };
    private readonly Button _check; private readonly Button _clear; private readonly Button _hint; private readonly Button _next;
    private int? _selectedOperation;

    public TrainingForm(AppSettings settings, bool exam, JsonStorage storage)
    {
        _settings = settings; _exam = exam; _storage = storage; _difficulty = settings.DefaultDifficulty;
        Text = exam ? "Проверка знаний" : "Обучение"; MinimumSize = new Size(980, 700); StartPosition = FormStartPosition.CenterScreen; AutoScaleMode = AutoScaleMode.Dpi; Font = new Font("Segoe UI", 10);
        _check = UiHelpers.Button("Проверить", (_, _) => Check()); _clear = UiHelpers.Button("Очистить / повторить", (_, _) => { if (_exercise?.IsSubmitted == true && !_exam) _exercise.Retry(); else _exercise?.Clear(); Render(); });
        _hint = UiHelpers.Button("Подсказка", (_, _) => Hint()); _next = UiHelpers.Button("Следующее задание", (_, _) => Next());
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10), BackColor = Color.AliceBlue };
        toolbar.Controls.AddRange([_check, _clear, _hint, _next, _status]); Controls.Add(_content); Controls.Add(toolbar);
        if (_exam) StartExam(); else ShowLearningSetup();
    }
    private void ShowLearningSetup()
    {
        _content.Controls.Clear(); var panel = Stack(); panel.Controls.Add(UiHelpers.Heading("Режим обучения"));
        var kinds = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 }; kinds.Items.AddRange(["Вычислите результат", "Заполните таблицу истинности", "Найдите пары", "Выберите все подходящие выражения"]); kinds.SelectedIndex = 0;
        var levels = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 }; levels.Items.AddRange(["Лёгкий", "Средний", "Сложный"]); levels.SelectedIndex = (int)_difficulty;
        panel.Controls.Add(new Label { Text = "Тип задания", AutoSize = true }); panel.Controls.Add(kinds); panel.Controls.Add(new Label { Text = "Сложность", AutoSize = true }); panel.Controls.Add(levels);
        panel.Controls.Add(UiHelpers.Button("Начать", (_, _) => { _kind = (ExerciseKind)kinds.SelectedIndex; _difficulty = (Difficulty)levels.SelectedIndex; NewExercise(); }));
        SetButtons(false, false, false, false); _content.Controls.Add(panel);
    }
    private void StartExam()
    {
        _session = new KnowledgeSession(_difficulty); _kind = _session.CurrentKind; NewExercise();
    }
    private void NewExercise()
    {
        _selectedOperation = null; _exercise = _factory.Create(_kind, _difficulty); _exercise.AnswerChanged += (_, _) => _check.Enabled = _exercise.CanSubmit; Render();
    }
    private void Render()
    {
        _content.SuspendLayout(); _content.Controls.Clear(); if (_exercise is null) return;
        var panel = Stack(); panel.Controls.Add(UiHelpers.Heading(_exam ? $"Задание {_session!.Position + 1} из 10" : UiHelpers.KindName(_exercise.Kind)));
        switch (_exercise)
        {
            case CalculationExercise e: RenderCalculation(panel, e); break;
            case TruthTableExercise e: RenderTable(panel, e); break;
            case MatchingExercise e: RenderMatching(panel, e); break;
            case SelectionExercise e: RenderSelection(panel, e); break;
        }
        if (_exercise.IsSubmitted) panel.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(820, 0), Font = new Font(Font, FontStyle.Bold), ForeColor = _exercise.IsCorrect == true ? Color.DarkGreen : Color.DarkRed, Text = (_exercise.IsCorrect == true ? "Верно!" : "Неверно.") + Environment.NewLine + _exercise.Explanation });
        _content.Controls.Add(panel); _content.ResumeLayout();
        SetButtons(_exercise.CanSubmit, !_exercise.IsSubmitted || (!_exam && _exercise.IsCorrect == false), !_exam && !_exercise.IsSubmitted, _exercise.IsSubmitted);
        _status.Text = _exam ? $"Сложность: {UiHelpers.DifficultyName(_difficulty)}" : "Повторные попытки разрешены";
    }
    private static FlowLayoutPanel Stack() => new() { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20) };
    private static Label Formula(string text) => new() { Text = text, AutoSize = true, Font = new Font("Segoe UI Symbol", 19, FontStyle.Bold), Margin = new Padding(8) };
    private void RenderCalculation(Control panel, CalculationExercise e)
    {
        panel.Controls.Add(Formula($"Выражение: {e.Expression}")); panel.Controls.Add(new Label { Text = string.Join(", ", e.Values.OrderBy(x => x.Key).Select(x => $"{x.Key} = {(x.Value ? 1 : 0)}")), AutoSize = true });
        var choices = new FlowLayoutPanel { AutoSize = true }; foreach (var value in new[] { false, true }) { var radio = new RadioButton { Text = value ? "1" : "0", AutoSize = true, Checked = e.Answer == value, Enabled = !e.IsSubmitted, Appearance = Appearance.Button, Padding = new Padding(20) }; radio.Click += (_, _) => e.SetAnswer(value); choices.Controls.Add(radio); } panel.Controls.Add(choices);
    }
    private void RenderTable(Control panel, TruthTableExercise e)
    {
        panel.Controls.Add(Formula(e.Expression.ToString())); var names = e.Expression.Variables.Order().ToArray();
        var grid = new DataGridView { Width = 600, Height = 300, AllowUserToAddRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        foreach (var n in names) grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = n, ReadOnly = true });
        var answer = new DataGridViewComboBoxColumn { HeaderText = "Результат", DataSource = new[] { "0", "1" }, FlatStyle = FlatStyle.Flat, ReadOnly = e.IsSubmitted }; grid.Columns.Add(answer);
        for (var i = 0; i < e.Rows.Count; i++) grid.Rows.Add(names.Select(n => (object)(e.Rows[i][n] ? "1" : "0")).Append(e.Answers[i] is null ? null! : e.Answers[i]!.Value ? "1" : "0").ToArray());
        grid.CurrentCellDirtyStateChanged += (_, _) => { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        grid.CellValueChanged += (_, a) => { if (a.RowIndex >= 0 && a.ColumnIndex == names.Length && !e.IsSubmitted) { var v = grid.Rows[a.RowIndex].Cells[a.ColumnIndex].Value?.ToString(); e.SetAnswer(a.RowIndex, v is null ? null : v == "1"); } };
        if (e.IsSubmitted) foreach (var row in e.IncorrectRows) grid.Rows[row].DefaultCellStyle.BackColor = Color.MistyRose; panel.Controls.Add(grid);
    }
    private void RenderMatching(Control panel, MatchingExercise e)
    {
        panel.Controls.Add(new Label { Text = "Выберите название слева, затем соответствующую таблицу справа. Щелчок по выбранному названию отменяет пару.", AutoSize = true });
        var area = new TableLayoutPanel { ColumnCount = 2, RowCount = 3, AutoSize = true, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single };
        for (var i = 0; i < 3; i++)
        {
            var oi = i; var paired = e.Pairs.TryGetValue(i, out var tableIndex); var left = new Button { Width = 320, Height = 100, Text = $"{(paired ? $"Пара {i + 1}: " : "")}{e.Operations[i].RussianName()} ({e.Operations[i].Symbol()})", Enabled = !e.IsSubmitted, BackColor = paired || _selectedOperation == i ? Color.FromArgb(_settings.HighlightArgb) : Color.White };
            left.Click += (_, _) => { if (e.Pairs.ContainsKey(oi)) e.Unpair(oi); else _selectedOperation = oi; Render(); }; area.Controls.Add(left, 0, i);
            var ti = i; var table = e.Tables[i]; var right = new Button { Width = 320, Height = 100, Font = new Font("Consolas", 10), Enabled = !e.IsSubmitted && !e.Pairs.ContainsValue(i), Text = $"A B | R\n0 0 | {Bit(table.Apply(false, false))}   0 1 | {Bit(table.Apply(false, true))}\n1 0 | {Bit(table.Apply(true, false))}   1 1 | {Bit(table.Apply(true, true))}" };
            var owner = e.Pairs.FirstOrDefault(x => x.Value == i); if (e.Pairs.ContainsValue(i)) { right.Text = $"Пара {owner.Key + 1}\n" + right.Text; right.BackColor = Color.FromArgb(_settings.HighlightArgb); }
            right.Click += (_, _) => { if (_selectedOperation.HasValue) { e.Pair(_selectedOperation.Value, ti); _selectedOperation = null; Render(); } }; area.Controls.Add(right, 1, i);
        }
        panel.Controls.Add(area);
    }
    private void RenderSelection(Control panel, SelectionExercise e)
    {
        panel.Controls.Add(new Label { Text = $"{string.Join(", ", e.Values.OrderBy(x => x.Key).Select(x => $"{x.Key} = {Bit(x.Value)}"))}. Выберите все выражения со значением {Bit(e.Target)}.", AutoSize = true });
        for (var i = 0; i < e.Options.Count; i++) { var index = i; var check = new CheckBox { Text = e.Options[i].ToString(), Font = new Font("Segoe UI Symbol", 14), AutoSize = true, Checked = e.Selected.Contains(i), Enabled = !e.IsSubmitted }; check.Click += (_, _) => e.Toggle(index); panel.Controls.Add(check); }
    }
    private void Check()
    {
        if (_exercise is null || !_exercise.Submit()) { Render(); return; } Render();
    }
    private void Hint()
    {
        if (_exercise is null || _exam) return; var text = _exercise switch { CalculationExercise e => $"Вычисляйте внутренние скобки первыми. {e.Expression.Explain(e.Values)}", TruthTableExercise => "Каждая строка — отдельный набор значений. Заполните все строки.", MatchingExercise => "Сравните строки 00, 01, 10 и 11 с определениями операций в справке.", SelectionExercise => "Вычислите каждое выражение отдельно и отметьте все совпадения.", _ => "" }; MessageBox.Show(text, "Подсказка");
    }
    private void Next()
    {
        if (_exercise is null) return;
        if (_exam)
        {
            if (!_session!.Record(_exercise)) return;
            if (_session.IsComplete) { CompleteExam(); return; }
            _kind = _session.CurrentKind;
        }
        NewExercise();
    }
    private void CompleteExam()
    {
        var result = _session!.Finish(); var all = _storage.LoadResults(); all.Add(result);
        if (!_storage.SaveResults(all, out var error)) MessageBox.Show($"Не удалось сохранить результаты: {error}");
        var details = string.Join(Environment.NewLine, result.ByType.Select(x => $"{UiHelpers.KindName(x.Key)}: {x.Value.Correct} из {x.Value.Total}"));
        MessageBox.Show($"Проверка завершена!\nПравильно: {result.Correct} из 10 ({result.Correct * 10}%)\nДлительность: {result.Duration:mm\\:ss}\n\n{details}", "Результат"); Close();
    }
    private void SetButtons(bool check, bool clear, bool hint, bool next) { _check.Enabled = check; _clear.Enabled = clear; _hint.Enabled = hint; _hint.Visible = !_exam; _next.Enabled = next; }
    private static int Bit(bool value) => value ? 1 : 0;
}
