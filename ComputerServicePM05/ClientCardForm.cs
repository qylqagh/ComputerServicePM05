using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class ClientCardForm : Form
{
    private readonly long? _clientId;
    private readonly TextBox _fullNameTextBox;
    private readonly TextBox _phoneTextBox;
    private readonly TextBox _emailTextBox;
    private readonly TableLayoutPanel _cardLayout;

    public ClientCardForm(long? clientId)
    {
        _clientId = clientId;

        Text = clientId is null ? "Новый клиент" : "Изменение клиента";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiKit.Background;
        Font = new Font("Segoe UI", 9.5F);

        _cardLayout = new TableLayoutPanel
        {
            Name = "ClientCardLayout",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = UiKit.Surface,
            Padding = new Padding(28, 30, 28, 28),
            Anchor = AnchorStyles.None
        };

        _cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        for (int i = 0; i < 8; i++)
        {
            _cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var titleLabel = new Label
        {
            Name = "ClientCardTitleLabel",
            Text = Text,
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiKit.TextDark,
            Margin = new Padding(0, 0, 0, 18)
        };

        _fullNameTextBox = UiKit.CreateFieldTextBox("FullNameTextBox", 100, 0, 16);
        _phoneTextBox = UiKit.CreateFieldTextBox("PhoneTextBox", 20, 1, 16);
        _emailTextBox = UiKit.CreateFieldTextBox("EmailTextBox", 100, 2, 22);

        _phoneTextBox.KeyPress += PhoneTextBox_KeyPress;

        var buttonsFlow = new FlowLayoutPanel
        {
            Name = "ClientCardButtonsPanel",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };

        var saveButton = UiKit.CreateButton("ClientSaveButton", "Сохранить", UiKit.Accent, Color.White, 140, 3);
        saveButton.Click += SaveButton_Click;

        var cancelButton = UiKit.CreateButton("ClientCancelButton", "Отмена", UiKit.Neutral, UiKit.TextDark, 110, 4);
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonsFlow.Controls.Add(saveButton);
        buttonsFlow.Controls.Add(cancelButton);

        _cardLayout.Controls.Add(titleLabel, 0, 0);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("FullNameLabel", "ФИО"), 0, 1);
        _cardLayout.Controls.Add(_fullNameTextBox, 0, 2);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("PhoneLabel", "Телефон"), 0, 3);
        _cardLayout.Controls.Add(_phoneTextBox, 0, 4);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("EmailLabel", "Email (необязательно)"), 0, 5);
        _cardLayout.Controls.Add(_emailTextBox, 0, 6);
        _cardLayout.Controls.Add(buttonsFlow, 0, 7);

        Controls.Add(_cardLayout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        if (clientId is not null)
        {
            LoadClient();
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

    private void LoadClient()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT full_name, phone, email FROM clients WHERE client_id = @id;";
            command.Parameters.AddWithValue("@id", _clientId.Value);

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                _fullNameTextBox.Text = reader.GetString(0);
                _phoneTextBox.Text = reader.GetString(1);
                _emailTextBox.Text = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            }
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void PhoneTextBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        var allowed = char.IsDigit(e.KeyChar)
            || char.IsControl(e.KeyChar)
            || e.KeyChar == '+'
            || e.KeyChar == '-'
            || e.KeyChar == '('
            || e.KeyChar == ')'
            || e.KeyChar == ' ';

        if (!allowed)
        {
            e.Handled = true;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        var fullName = _fullNameTextBox.Text.Trim();
        var phone = _phoneTextBox.Text.Trim();
        var email = _emailTextBox.Text.Trim();

        if (fullName.Length < 2)
        {
            UiKit.ShowWarning(this, "Укажите ФИО клиента: минимум 2 символа.", "Проверка данных");
            _fullNameTextBox.Focus();
            return;
        }

        if (phone.Length < 6)
        {
            UiKit.ShowWarning(this, "Укажите телефон клиента: минимум 6 цифр.", "Проверка данных");
            _phoneTextBox.Focus();
            return;
        }

        if (email.Length > 0 && (!email.Contains('@') || !email.Contains('.')))
        {
            UiKit.ShowWarning(this, "Проверьте формат email, пример: ivanov@mail.ru", "Проверка данных");
            _emailTextBox.Focus();
            return;
        }

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            if (_clientId is null)
            {
                command.CommandText = "INSERT INTO clients (full_name, phone, email) VALUES (@fullName, @phone, @email);";
            }
            else
            {
                command.CommandText = "UPDATE clients SET full_name = @fullName, phone = @phone, email = @email WHERE client_id = @id;";
                command.Parameters.AddWithValue("@id", _clientId.Value);
            }

            command.Parameters.AddWithValue("@fullName", fullName);
            command.Parameters.AddWithValue("@phone", phone);
            command.Parameters.AddWithValue("@email", email.Length == 0 ? DBNull.Value : email);
            command.ExecuteNonQuery();

            UiKit.ShowInfo(this, _clientId is null ? "Клиент добавлен в базу." : "Изменения сохранены.", "Готово");

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