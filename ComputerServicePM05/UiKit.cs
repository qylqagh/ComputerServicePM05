using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerServicePM05;

public static class UiKit
{
    public static readonly Color Background = Color.FromArgb(248, 250, 252);
    public static readonly Color Surface = Color.White;
    public static readonly Color Accent = Color.FromArgb(59, 130, 246);
    public static readonly Color AccentHover = Color.FromArgb(37, 99, 235);
    public static readonly Color AccentPressed = Color.FromArgb(29, 78, 216);
    public static readonly Color Neutral = Color.FromArgb(226, 232, 240);
    public static readonly Color NeutralHover = Color.FromArgb(203, 213, 225);
    public static readonly Color DangerFore = Color.FromArgb(185, 28, 28);
    public static readonly Color TextDark = Color.FromArgb(15, 23, 42);
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    public static readonly Color TextSlate = Color.FromArgb(51, 65, 85);
    public static readonly Color HeaderBack = Color.FromArgb(241, 245, 249);
    public static readonly Color GridLine = Color.FromArgb(226, 232, 240);
    public static readonly Color SelectionBack = Color.FromArgb(219, 234, 254);

    public static void StyleGrid(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Surface;
        grid.GridColor = GridLine;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnHeadersHeight = 40;
        grid.RowTemplate.Height = 34;
        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBack;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSlate;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.DefaultCellStyle.Padding = new Padding(10, 0, 6, 0);
        grid.DefaultCellStyle.SelectionBackColor = SelectionBack;
        grid.DefaultCellStyle.SelectionForeColor = TextDark;
    }

    public static Button CreateButton(string name, string text, Color backColor, Color foreColor, int width, int tabIndex)
    {
        var button = new Button
        {
            Name = name,
            Text = text,
            Width = width,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = foreColor,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0),
            TabIndex = tabIndex
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.08F);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.08F);

        return button;
    }

    public static Label CreateFieldLabel(string name, string text)
    {
        return new Label
        {
            Name = name,
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = TextSlate,
            Margin = new Padding(0, 0, 0, 6)
        };
    }

    public static TextBox CreateFieldTextBox(string name, int maxLength, int tabIndex, int bottomMargin)
    {
        return new TextBox
        {
            Name = name,
            Width = 340,
            MaxLength = maxLength,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10.5F),
            Margin = new Padding(0, 0, 0, bottomMargin),
            TabIndex = tabIndex
        };
    }

    public static ComboBox CreateFieldComboBox(string name, int tabIndex, int bottomMargin)
    {
        return new ComboBox
        {
            Name = name,
            Width = 340,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10.5F),
            Margin = new Padding(0, 0, 0, bottomMargin),
            TabIndex = tabIndex
        };
    }

    public static void ShowWarning(IWin32Window owner, string text, string title)
    {
        MessageBox.Show(owner, text, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static void ShowInfo(IWin32Window owner, string text, string title)
    {
        MessageBox.Show(owner, text, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static void ShowDatabaseError(IWin32Window owner)
    {
        MessageBox.Show(
            owner,
            "Не удалось обратиться к базе данных. Проверьте подключение и повторите действие.",
            "Ошибка базы данных",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    public static void ShowUnexpectedError(IWin32Window owner)
    {
        MessageBox.Show(
            owner,
            "Произошла непредвиденная ошибка. Приложение продолжит работу.",
            "Ошибка",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    public static bool Confirm(IWin32Window owner, string text, string title)
    {
        return MessageBox.Show(owner, text, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }
}