using System;
using System.Drawing;
using System.Windows.Forms;
using ComputerServicePM05.Models;

namespace ComputerServicePM05;

public abstract class EntityListForm : Form
{
    protected readonly UserSession Session;
    protected readonly DataGridView Grid = new DataGridView();
    protected readonly TextBox SearchTextBox;
    protected readonly FlowLayoutPanel ToolbarActionsPanel = new FlowLayoutPanel();

    private readonly Label _emptyLabel;
    private readonly string _emptyBaseText;
    private readonly string _emptySearchText;

    protected EntityListForm(string title, UserSession session, string emptyBaseText, string emptySearchText)
    {
        Session = session;
        _emptyBaseText = emptyBaseText;
        _emptySearchText = emptySearchText;

        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UiKit.Background;
        Font = new Font("Segoe UI", 9.5F);

        var workingArea = Screen.PrimaryScreen.WorkingArea;

        MinimumSize = new Size(960, 600);
        Width = Math.Min(1350, (int)(workingArea.Width * 0.78));
        Height = Math.Min(800, (int)(workingArea.Height * 0.8));

        var toolbarLayout = new TableLayoutPanel
        {
            Name = "ToolbarLayout",
            Dock = DockStyle.Top,
            Height = 64,
            ColumnCount = 3,
            Padding = new Padding(16, 14, 16, 10),
            BackColor = UiKit.Background
        };

        toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        ToolbarActionsPanel.Name = "ToolbarActionsPanel";
        ToolbarActionsPanel.AutoSize = true;
        ToolbarActionsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ToolbarActionsPanel.FlowDirection = FlowDirection.LeftToRight;
        ToolbarActionsPanel.WrapContents = false;
        ToolbarActionsPanel.Margin = new Padding(0);

        SearchTextBox = new TextBox
        {
            Name = "SearchTextBox",
            Width = 240,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10F),
            Margin = new Padding(0, 2, 12, 0),
            TabIndex = 0
        };

        SearchTextBox.TextChanged += (_, _) => LoadData();
        ToolbarActionsPanel.Controls.Add(SearchTextBox);

        var closeButton = UiKit.CreateButton("CloseButton", "Закрыть", UiKit.Neutral, UiKit.TextDark, 100, 90);
        closeButton.Click += (_, _) => Close();

        toolbarLayout.Controls.Add(ToolbarActionsPanel, 0, 0);
        toolbarLayout.Controls.Add(closeButton, 2, 0);

        var hostPanel = new Panel
        {
            Name = "GridHostPanel",
            Dock = DockStyle.Fill,
            BackColor = UiKit.Surface
        };

        Grid.Name = "EntityGrid";
        Grid.Dock = DockStyle.Fill;
        UiKit.StyleGrid(Grid);

        _emptyLabel = new Label
        {
            Name = "EmptyLabel",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = UiKit.TextMuted,
            Font = new Font("Segoe UI", 10.5F),
            Visible = false
        };

        hostPanel.Controls.Add(Grid);
        hostPanel.Controls.Add(_emptyLabel);

        Controls.Add(hostPanel);
        Controls.Add(toolbarLayout);

        CancelButton = closeButton;
    }

    protected bool IsAdmin => Session.Role == "Администратор";

    protected abstract void LoadData();

    protected Button AddToolbarButton(string name, string text, Color backColor, Color foreColor, Action onClick, int tabIndex)
    {
        var button = UiKit.CreateButton(name, text, backColor, foreColor, 110, tabIndex);
        button.Click += (_, _) => onClick();
        ToolbarActionsPanel.Controls.Add(button);
        return button;
    }

    protected void ApplyEmptyState(bool hasRows)
    {
        Grid.Visible = hasRows;
        _emptyLabel.Visible = !hasRows;

        if (!hasRows)
        {
            var query = SearchTextBox.Text.Trim();
            _emptyLabel.Text = string.IsNullOrWhiteSpace(query)
                ? _emptyBaseText
                : string.Format(_emptySearchText, query);
        }
    }

    protected long? GetSelectedId(string idColumnName, string entityName)
    {
        if (Grid.SelectedRows.Count == 0)
        {
            UiKit.ShowWarning(this, $"Сначала выберите строку с записью \"{entityName}\" в таблице.", "Нет выделения");
            return null;
        }

        return Convert.ToInt64(Grid.SelectedRows[0].Cells[idColumnName].Value);
    }

    protected string GetSelectedText(string columnName)
    {
        return Grid.SelectedRows[0].Cells[columnName].Value?.ToString() ?? string.Empty;
    }
}