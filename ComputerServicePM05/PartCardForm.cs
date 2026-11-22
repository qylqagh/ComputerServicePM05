using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class PartCardForm : Form
{
    private readonly long? _partId;
    private readonly TextBox _articleTextBox;
    private readonly TextBox _nameTextBox;
    private readonly TextBox _priceTextBox;
    private readonly NumericUpDown _quantityNumeric;
    private readonly TableLayoutPanel _cardLayout;

    public PartCardForm(long? partId)
    {
        _partId = partId;

        Text = partId is null ? "Новая запчасть" : "Изменение запчасти";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiKit.Background;
        Font = new Font("Segoe UI", 9.5F);

        _cardLayout = new TableLayoutPanel
        {
            Name = "PartCardLayout",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = UiKit.Surface,
            Padding = new Padding(28, 30, 28, 28),
            Anchor = AnchorStyles.None
        };

        _cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        for (int i = 0; i < 10; i++)
        {
            _cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var titleLabel = new Label
        {
            Name = "PartCardTitleLabel",
            Text = Text,
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiKit.TextDark,
            Margin = new Padding(0, 0, 0, 18)
        };

        _articleTextBox = UiKit.CreateFieldTextBox("ArticleTextBox", 40, 0, 16);
        _nameTextBox = UiKit.CreateFieldTextBox("PartNameTextBox", 100, 1, 16);
        _priceTextBox = UiKit.CreateFieldTextBox("PriceTextBox", 12, 2, 16);
        _priceTextBox.KeyPress += DecimalTextBox_KeyPress;

        _quantityNumeric = new NumericUpDown
        {
            Name = "QuantityNumeric",
            Width = 340,
            Minimum = 0,
            Maximum = 1000000,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10.5F),
            Margin = new Padding(0, 0, 0, 22),
            TabIndex = 3
        };

        var buttonsFlow = new FlowLayoutPanel
        {
            Name = "PartCardButtonsPanel",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };

        var saveButton = UiKit.CreateButton("PartSaveButton", "Сохранить", UiKit.Accent, Color.White, 140, 4);
        saveButton.Click += SaveButton_Click;

        var cancelButton = UiKit.CreateButton("PartCancelButton", "Отмена", UiKit.Neutral, UiKit.TextDark, 110, 5);
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonsFlow.Controls.Add(saveButton);
        buttonsFlow.Controls.Add(cancelButton);

        _cardLayout.Controls.Add(titleLabel, 0, 0);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("ArticleLabel", "Артикул"), 0, 1);
        _cardLayout.Controls.Add(_articleTextBox, 0, 2);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("PartNameLabel", "Название"), 0, 3);
        _cardLayout.Controls.Add(_nameTextBox, 0, 4);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("PriceLabel", "Цена, руб"), 0, 5);
        _cardLayout.Controls.Add(_priceTextBox, 0, 6);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("QuantityLabel", "Остаток на складе"), 0, 7);
        _cardLayout.Controls.Add(_quantityNumeric, 0, 8);
        _cardLayout.Controls.Add(buttonsFlow, 0, 9);

        Controls.Add(_cardLayout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        if (partId is not null)
        {
            LoadPart();
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        ClientSize = new Size(_cardLayout.Width + 90, _cardLayout.Height + 80);

        _cardLayout.Location = new Point(
            (ClientSize.Width - _cardLayout.Width) / 2,
            (ClientSize.Height - _cardLayout.Height) / 2);
    }

    private void LoadPart()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT article, part_name, price, quantity FROM parts WHERE part_id = @id;";
            command.Parameters.AddWithValue("@id", _partId.Value);

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                _articleTextBox.Text = reader.GetString(0);
                _nameTextBox.Text = reader.GetString(1);
                _priceTextBox.Text = reader.GetDouble(2).ToString("0.##", CultureInfo.InvariantCulture);
                _quantityNumeric.Value = reader.GetInt64(3);
            }
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void DecimalTextBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        var textBox = (TextBox)sender!;

        var allowed = char.IsDigit(e.KeyChar)
            || char.IsControl(e.KeyChar)
            || ((e.KeyChar == '.' || e.KeyChar == ',') && !textBox.Text.Contains('.') && !textBox.Text.Contains(','));

        if (!allowed)
        {
            e.Handled = true;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        var article = _articleTextBox.Text.Trim();
        var name = _nameTextBox.Text.Trim();

        if (article.Length < 2)
        {
            UiKit.ShowWarning(this, "Укажите артикул: минимум 2 символа.", "Проверка данных");
            _articleTextBox.Focus();
            return;
        }

        if (name.Length < 2)
        {
            UiKit.ShowWarning(this, "Укажите название запчасти: минимум 2 символа.", "Проверка данных");
            _nameTextBox.Focus();
            return;
        }

        if (!decimal.TryParse(_priceTextBox.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price < 0)
        {
            UiKit.ShowWarning(this, "Укажите цену в виде числа, пример: 1500 или 1500.50", "Проверка данных");
            _priceTextBox.Focus();
            return;
        }

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM parts WHERE article = @article AND (@id IS NULL OR part_id <> @id);";
            checkCommand.Parameters.AddWithValue("@article", article);
            checkCommand.Parameters.AddWithValue("@id", _partId is null ? DBNull.Value : _partId.Value);

            if (Convert.ToInt64(checkCommand.ExecuteScalar()) > 0)
            {
                UiKit.ShowWarning(this, "Запчасть с таким артикулом уже существует. Укажите уникальный артикул.", "Проверка данных");
                _articleTextBox.Focus();
                return;
            }

            using var command = connection.CreateCommand();

            if (_partId is null)
            {
                command.CommandText = "INSERT INTO parts (article, part_name, price, quantity) VALUES (@article, @name, @price, @quantity);";
            }
            else
            {
                command.CommandText = "UPDATE parts SET article = @article, part_name = @name, price = @price, quantity = @quantity WHERE part_id = @id;";
                command.Parameters.AddWithValue("@id", _partId.Value);
            }

            command.Parameters.AddWithValue("@article", article);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@price", (double)price);
            command.Parameters.AddWithValue("@quantity", (long)_quantityNumeric.Value);
            command.ExecuteNonQuery();

            UiKit.ShowInfo(this, _partId is null ? "Запчасть добавлена на склад." : "Изменения сохранены.", "Готово");

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (SqliteException)
        {
            UiKit.ShowDatabaseError(this);
        }
        catch (Exception)
        {
            UiKit.ShowUnexpectedError(this);
        }
    }
}