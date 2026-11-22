using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class DeviceCardForm : Form
{
    private readonly long? _deviceId;
    private readonly ComboBox _categoryComboBox;
    private readonly TextBox _manufacturerTextBox;
    private readonly TextBox _modelTextBox;
    private readonly TextBox _serialTextBox;
    private readonly TableLayoutPanel _cardLayout;

    public DeviceCardForm(long? deviceId)
    {
        _deviceId = deviceId;

        Text = deviceId is null ? "Новое устройство" : "Изменение устройства";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiKit.Background;
        Font = new Font("Segoe UI", 9.5F);

        _cardLayout = new TableLayoutPanel
        {
            Name = "DeviceCardLayout",
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
            Name = "DeviceCardTitleLabel",
            Text = Text,
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiKit.TextDark,
            Margin = new Padding(0, 0, 0, 18)
        };

        _categoryComboBox = UiKit.CreateFieldComboBox("CategoryComboBox", 0, 16);
        _manufacturerTextBox = UiKit.CreateFieldTextBox("ManufacturerTextBox", 60, 1, 16);
        _modelTextBox = UiKit.CreateFieldTextBox("ModelTextBox", 60, 2, 16);
        _serialTextBox = UiKit.CreateFieldTextBox("SerialTextBox", 60, 3, 22);

        var buttonsFlow = new FlowLayoutPanel
        {
            Name = "DeviceCardButtonsPanel",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };

        var saveButton = UiKit.CreateButton("DeviceSaveButton", "Сохранить", UiKit.Accent, Color.White, 140, 4);
        saveButton.Click += SaveButton_Click;

        var cancelButton = UiKit.CreateButton("DeviceCancelButton", "Отмена", UiKit.Neutral, UiKit.TextDark, 110, 5);
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonsFlow.Controls.Add(saveButton);
        buttonsFlow.Controls.Add(cancelButton);

        _cardLayout.Controls.Add(titleLabel, 0, 0);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("CategoryLabel", "Категория"), 0, 1);
        _cardLayout.Controls.Add(_categoryComboBox, 0, 2);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("ManufacturerLabel", "Производитель"), 0, 3);
        _cardLayout.Controls.Add(_manufacturerTextBox, 0, 4);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("ModelLabel", "Модель"), 0, 5);
        _cardLayout.Controls.Add(_modelTextBox, 0, 6);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("SerialLabel", "Серийный номер (необязательно)"), 0, 7);
        _cardLayout.Controls.Add(_serialTextBox, 0, 8);
        _cardLayout.Controls.Add(buttonsFlow, 0, 9);

        Controls.Add(_cardLayout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        LoadCategories();

        if (deviceId is not null)
        {
            LoadDevice();
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

    private void LoadCategories()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT device_category_id, category_name FROM device_categories ORDER BY category_name;";

            var table = new DataTable();
            table.Load(command.ExecuteReader());

            _categoryComboBox.DataSource = table;
            _categoryComboBox.DisplayMember = "category_name";
            _categoryComboBox.ValueMember = "device_category_id";
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void LoadDevice()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT device_category_id, manufacturer, model, serial_number FROM devices WHERE device_id = @id;";
            command.Parameters.AddWithValue("@id", _deviceId.Value);

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                _categoryComboBox.SelectedValue = reader.GetInt64(0);
                _manufacturerTextBox.Text = reader.GetString(1);
                _modelTextBox.Text = reader.GetString(2);
                _serialTextBox.Text = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            }
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        var manufacturer = _manufacturerTextBox.Text.Trim();
        var model = _modelTextBox.Text.Trim();
        var serial = _serialTextBox.Text.Trim();

        if (_categoryComboBox.SelectedValue is null)
        {
            UiKit.ShowWarning(this, "Выберите категорию устройства из списка.", "Проверка данных");
            _categoryComboBox.Focus();
            return;
        }

        if (manufacturer.Length < 2)
        {
            UiKit.ShowWarning(this, "Укажите производителя: минимум 2 символа.", "Проверка данных");
            _manufacturerTextBox.Focus();
            return;
        }

        if (model.Length < 1)
        {
            UiKit.ShowWarning(this, "Укажите модель устройства.", "Проверка данных");
            _modelTextBox.Focus();
            return;
        }

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            if (_deviceId is null)
            {
                command.CommandText = "INSERT INTO devices (device_category_id, manufacturer, model, serial_number) VALUES (@categoryId, @manufacturer, @model, @serial);";
            }
            else
            {
                command.CommandText = "UPDATE devices SET device_category_id = @categoryId, manufacturer = @manufacturer, model = @model, serial_number = @serial WHERE device_id = @id;";
                command.Parameters.AddWithValue("@id", _deviceId.Value);
            }

            command.Parameters.AddWithValue("@categoryId", Convert.ToInt64(_categoryComboBox.SelectedValue));
            command.Parameters.AddWithValue("@manufacturer", manufacturer);
            command.Parameters.AddWithValue("@model", model);
            command.Parameters.AddWithValue("@serial", serial.Length == 0 ? DBNull.Value : serial);
            command.ExecuteNonQuery();

            UiKit.ShowInfo(this, _deviceId is null ? "Устройство добавлено в базу." : "Изменения сохранены.", "Готово");

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