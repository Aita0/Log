using LogicTrainer.Domain;
using LogicTrainer.Services;

namespace LogicTrainer.UI;

public sealed class MainForm : Form
{
    private readonly JsonStorage _storage;
    private readonly AppSettings _settings;
    public MainForm(JsonStorage storage)
    {
        _storage = storage; _settings = storage.LoadSettings();
        Text = "Тренажёр логических операций"; MinimumSize = new Size(900, 650); StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi; Font = new Font("Segoe UI", 10); BackColor = Color.WhiteSmoke;
        ShowMenu();
    }
    private void ShowMenu()
    {
        Controls.Clear();
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(70) };
        panel.Controls.Add(UiHelpers.Heading("Тренажёр логических операций", 24));
        panel.Controls.Add(UiHelpers.Button("Обучение", (_, _) => Open(new TrainingForm(_settings, false, _storage))));
        panel.Controls.Add(UiHelpers.Button("Проверка знаний", (_, _) => StartKnowledgeCheck()));
        panel.Controls.Add(UiHelpers.Button("Справка по операциям", (_, _) => Open(new HelpForm())));
        panel.Controls.Add(UiHelpers.Button("Настройки", (_, _) => Open(new SettingsForm(_settings, _storage))));
        panel.Controls.Add(UiHelpers.Button("Результаты", (_, _) => Open(new ResultsForm(_storage))));
        panel.Controls.Add(UiHelpers.Button("Выход", (_, _) => Close())); Controls.Add(panel);
    }
    private void StartKnowledgeCheck()
    {
        var answer = MessageBox.Show(
            this,
            $"Начать проверку из 10 заданий? Сложность: {UiHelpers.DifficultyName(_settings.DefaultDifficulty)}.",
            "Проверка знаний",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
            return;

        Open(new TrainingForm(_settings, true, _storage));
    }

    private void Open(Form form)
    {
        Hide();
        form.FormClosed += (_, _) =>
        {
            form.Dispose();
            if (!IsDisposed && !Disposing)
                Show();
        };
        form.Show(this);
    }
}
