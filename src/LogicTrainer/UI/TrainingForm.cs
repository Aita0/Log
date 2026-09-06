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
        var kinds = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 }; kinds.Items.AddRange(["Вычислите результат", "Заполните таблицу истинности", "Найдите пары", "Выберите все подходящие выражения"]); kinds.SelectedIndex = 0;
        var levels = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 }; levels.Items.AddRange(["Лёгкий", "Средний", "Сложный"]); levels.SelectedIndex = (int)_difficulty;
        var levelCaption = new Label { Text = "Сложность", AutoSize = true };
        var matchingCaption = new Label { Text = "Базовое упражнение: сопоставление операций и таблиц истинности", AutoSize = true, Visible = false, ForeColor = Color.DimGray, Font = new Font(Font, FontStyle.Italic), Margin = new Padding(3, 8, 3, 8) };
        kinds.SelectedIndexChanged += (_, _) =>
        {
            var isMatching = kinds.SelectedIndex == (int)ExerciseKind.Matching;
            levels.Enabled = !isMatching;
            levelCaption.Enabled = !isMatching;
            matchingCaption.Visible = isMatching;
        };
        panel.Controls.Add(new Label { Text = "Тип задания", AutoSize = true }); panel.Controls.Add(kinds); panel.Controls.Add(levelCaption); panel.Controls.Add(levels); panel.Controls.Add(matchingCaption);
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
        panel.Controls.Add(new Label { Text = "Базовое упражнение: сопоставление операций и таблиц истинности", AutoSize = true, ForeColor = Color.DimGray, Font = new Font(Font, FontStyle.Italic), Margin = new Padding(3, 3, 3, 10) });
        var area = new TableLayoutPanel { ColumnCount = 2, RowCount = 3, Width = 850, Height = 480, Margin = new Padding(0, 8, 0, 8), GrowStyle = TableLayoutPanelGrowStyle.FixedSize };
        area.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        area.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var row = 0; row < 3; row++) area.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3));
        for (var i = 0; i < 3; i++)
        {
            var operationIndex = i;
            var paired = e.Pairs.ContainsKey(operationIndex);
            var pending = _selectedOperation == operationIndex;
            var operationBorder = paired ? PairColor(operationIndex) : pending ? Color.DarkOrange : Color.LightGray;
            var operationTitle = paired ? $"Пара №{operationIndex + 1}" : pending ? "Выбрано — укажите таблицу" : "Операция";
            var operationCard = CreateCard(operationBorder, operationTitle);
            var operationButton = new Button { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }, BackColor = Color.White, Enabled = !e.IsSubmitted, Text = $"{e.Operations[i].RussianName()}\n({e.Operations[i].Symbol()})", TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Symbol", 12, FontStyle.Bold), Padding = new Padding(16), AutoEllipsis = false };
            operationButton.Click += (_, _) => { if (e.Pairs.ContainsKey(operationIndex)) e.Unpair(operationIndex); else _selectedOperation = operationIndex; Render(); };
            operationCard.Controls.Add(operationButton, 0, 1);
            area.Controls.Add(operationCard, 0, i);

            var tableIndex = i;
            var pairOwner = e.Pairs.FirstOrDefault(pair => pair.Value == tableIndex);
            var tableIsPaired = e.Pairs.ContainsValue(tableIndex);
            var tableCard = CreateCard(tableIsPaired ? PairColor(pairOwner.Key) : Color.LightGray, tableIsPaired ? $"Пара №{pairOwner.Key + 1}" : "Таблица истинности");
            var truthTable = CreateTruthTable(e.Tables[tableIndex]);
            SetClickHandler(truthTable, (_, _) =>
            {
                if (!e.IsSubmitted && !tableIsPaired && _selectedOperation.HasValue)
                {
                    e.Pair(_selectedOperation.Value, tableIndex); _selectedOperation = null; Render();
                }
            });
            tableCard.Controls.Add(truthTable, 0, 1);
            area.Controls.Add(tableCard, 1, i);
        }
        panel.Controls.Add(area);
    }

    private TableLayoutPanel CreateCard(Color borderColor, string caption)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = borderColor, Padding = new Padding(3), Margin = new Padding(8), RowCount = 2, ColumnCount = 1 };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font, FontStyle.Bold), Padding = new Padding(4) }, 0, 0);
        return card;
    }

    private static TableLayoutPanel CreateTruthTable(LogicOperation operation)
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, ColumnCount = 3, RowCount = 5, Padding = new Padding(14, 6, 14, 8), Cursor = Cursors.Hand, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single };
        for (var column = 0; column < 3; column++) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        for (var row = 0; row < 5; row++) table.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        AddTruthCell(table, "A", 0, 0, true); AddTruthCell(table, "B", 1, 0, true); AddTruthCell(table, "R", 2, 0, true);
        var values = new[] { (false, false), (false, true), (true, false), (true, true) };
        for (var row = 0; row < values.Length; row++)
        {
            AddTruthCell(table, Bit(values[row].Item1).ToString(), 0, row + 1);
            AddTruthCell(table, Bit(values[row].Item2).ToString(), 1, row + 1);
            AddTruthCell(table, Bit(operation.Apply(values[row].Item1, values[row].Item2)).ToString(), 2, row + 1);
        }
        return table;
    }

    private static void AddTruthCell(TableLayoutPanel table, string text, int column, int row, bool heading = false) =>
        table.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Margin = Padding.Empty, BackColor = heading ? Color.AliceBlue : Color.White, Font = new Font("Segoe UI", 10, heading ? FontStyle.Bold : FontStyle.Regular) }, column, row);

    private static void SetClickHandler(Control control, EventHandler handler)
    {
        control.Click += handler;
        foreach (Control child in control.Controls) SetClickHandler(child, handler);
    }

    private Color PairColor(int pairIndex) => pairIndex switch
    {
        0 => Color.FromArgb(_settings.HighlightArgb),
        1 => Color.MediumSeaGreen,
        _ => Color.MediumPurple
    };
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
