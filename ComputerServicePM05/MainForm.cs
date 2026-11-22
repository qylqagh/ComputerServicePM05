using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ComputerServicePM05.Models;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class MainForm : Form
{
    private readonly UserSession _userSession;

    private readonly FlowLayoutPanel _menuPanel = new FlowLayoutPanel
    {
        Name = "MainMenuPanel",
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = Color.FromArgb(30, 41, 59),
        Padding = new Padding(0, 4, 0, 4)
    };

    private readonly Label _clientsCountLabel = new Label();
    private readonly Label _devicesCountLabel = new Label();
    private readonly Label _activeOrdersCountLabel = new Label();
    private readonly Label _partsCountLabel = new Label();
    private readonly DataGridView _recentOrdersGrid = new DataGridView();
    private readonly Label _noOrdersLabel = new Label();

    public bool LogoutRequested { get; private set; }

    public MainForm(UserSession userSession)
    {
        _userSession = userSession;

        Text = "Сервисный центр — Главная";
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(248, 250, 252);
        Font = new Font("Segoe UI", 9.5F);

        var workingArea = Screen.PrimaryScreen.WorkingArea;

        MinimumSize = new Size(1100, 700);
        Width = Math.Min(1500, (int)(workingArea.Width * 0.85));
        Height = Math.Min(860, (int)(workingArea.Height * 0.85));

        BuildMenu();

        var rootLayout = new TableLayoutPanel
        {
            Name = "MainRootLayout",
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264F));
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        rootLayout.Controls.Add(CreateSidebarLayout(), 0, 0);
        rootLayout.Controls.Add(CreateContentLayout(), 1, 0);

        Controls.Add(rootLayout);

        LoadDashboardData();
    }

    private void BuildMenu()
    {
        AddMenuButton("Клиенты", () => OpenSectionForm(session => new ClientsForm(session)));
        AddMenuButton("Устройства", () => OpenSectionForm(session => new DevicesForm(session)));
        AddMenuButton("Заявки на ремонт", () => OpenSectionForm(session => new RepairOrdersForm(session)));
        AddMenuButton("Запчасти", () => OpenSectionForm(session => new PartsForm(session)));

        if (_userSession.Role == "Администратор")
        {
            AddMenuButton("Пользователи", () => OpenSectionForm(session => new UsersForm(session)));
        }
    }

    private void AddMenuButton(string text, Action action)
    {
        var button = CreateMenuButton(text);
        button.TabIndex = _menuPanel.Controls.Count;
        button.Click += (_, _) => action();
        _menuPanel.Controls.Add(button);
    }

    private Button CreateMenuButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Width = 228,
            Height = 42,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Segoe UI", 10F),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Margin = new Padding(0, 2, 0, 2),
            Cursor = Cursors.Hand,
            AutoSize = false
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 85);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(71, 85, 105);

        return button;
    }

    private TableLayoutPanel CreateSidebarLayout()
    {
        var sidebarLayout = new TableLayoutPanel
        {
            Name = "MainSidebarLayout",
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(18, 20, 18, 16)
        };

        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 13F));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var brandLabel = new Label
        {
            Name = "BrandLabel",
            Text = "Сервисный центр",
            AutoSize = true,
            MaximumSize = new Size(228, 0),
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 2)
        };

        var brandSubtitleLabel = new Label
        {
            Name = "BrandSubtitleLabel",
            Text = "Ремонт и учет техники",
            AutoSize = true,
            MaximumSize = new Size(228, 0),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Margin = new Padding(0, 0, 0, 16)
        };

        var userNameLabel = new Label
        {
            Name = "UserNameLabel",
            Text = _userSession.FullName,
            AutoSize = true,
            MaximumSize = new Size(228, 0),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Margin = new Padding(0, 0, 0, 2)
        };

        var userRoleLabel = new Label
        {
            Name = "UserRoleLabel",
            Text = _userSession.Role,
            AutoSize = true,
            MaximumSize = new Size(228, 0),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Margin = new Padding(0, 0, 0, 0)
        };

        var separatorPanel = new Panel
        {
            Name = "SidebarSeparatorPanel",
            Height = 1,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(51, 65, 85),
            Margin = new Padding(0, 6, 0, 6)
        };

        var logoutButton = CreateMenuButton("Выйти");
        logoutButton.Name = "LogoutButton";
        logoutButton.ForeColor = Color.FromArgb(252, 165, 165);
        logoutButton.Margin = new Padding(0, 12, 0, 0);
        logoutButton.TabIndex = 99;
        logoutButton.Click += (_, _) => RequestLogout();

        sidebarLayout.Controls.Add(brandLabel, 0, 0);
        sidebarLayout.Controls.Add(brandSubtitleLabel, 0, 1);
        sidebarLayout.Controls.Add(userNameLabel, 0, 2);
        sidebarLayout.Controls.Add(userRoleLabel, 0, 3);
        sidebarLayout.Controls.Add(separatorPanel, 0, 4);
        sidebarLayout.Controls.Add(_menuPanel, 0, 5);
        sidebarLayout.Controls.Add(logoutButton, 0, 6);

        return sidebarLayout;
    }

    private TableLayoutPanel CreateContentLayout()
    {
        var contentLayout = new TableLayoutPanel
        {
            Name = "MainContentLayout",
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(36, 34, 36, 36)
        };

        contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var titleLabel = new Label
        {
            Name = "ContentTitleLabel",
            Text = $"Добро пожаловать, {_userSession.FullName}",
            AutoSize = true,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 0, 0, 6)
        };

        var subtitleLabel = new Label
        {
            Name = "ContentSubtitleLabel",
            Text = "Сводка по сервисному центру на текущий момент.",
            AutoSize = true,
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Margin = new Padding(0, 0, 0, 22)
        };

        var cardsFlow = new FlowLayoutPanel
        {
            Name = "DashboardCardsPanel",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 6)
        };

        cardsFlow.Controls.Add(CreateStatCard("Клиентов в базе", _clientsCountLabel));
        cardsFlow.Controls.Add(CreateStatCard("Устройств на учете", _devicesCountLabel));
        cardsFlow.Controls.Add(CreateStatCard("Активных заявок", _activeOrdersCountLabel));
        cardsFlow.Controls.Add(CreateStatCard("Запчастей на складе", _partsCountLabel));

        var gridTitleLabel = new Label
        {
            Name = "RecentOrdersTitleLabel",
            Text = "Последние заявки",
            AutoSize = true,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 16, 0, 10)
        };

        var hostPanel = new Panel
        {
            Name = "RecentOrdersHostPanel",
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        _recentOrdersGrid.Name = "RecentOrdersGrid";
        _recentOrdersGrid.Dock = DockStyle.Fill;
        UiKit.StyleGrid(_recentOrdersGrid);

        _noOrdersLabel.Name = "NoOrdersLabel";
        _noOrdersLabel.Dock = DockStyle.Fill;
        _noOrdersLabel.TextAlign = ContentAlignment.MiddleCenter;
        _noOrdersLabel.ForeColor = UiKit.TextMuted;
        _noOrdersLabel.Font = new Font("Segoe UI", 10.5F);
        _noOrdersLabel.Text = "Заявок пока нет. Создайте первую заявку в разделе «Заявки на ремонт».";
        _noOrdersLabel.Visible = false;

        hostPanel.Controls.Add(_recentOrdersGrid);
        hostPanel.Controls.Add(_noOrdersLabel);

        contentLayout.Controls.Add(titleLabel, 0, 0);
        contentLayout.Controls.Add(subtitleLabel, 0, 1);
        contentLayout.Controls.Add(cardsFlow, 0, 2);
        contentLayout.Controls.Add(gridTitleLabel, 0, 3);
        contentLayout.Controls.Add(hostPanel, 0, 4);

        return contentLayout;
    }

    private Panel CreateStatCard(string caption, Label valueLabel)
    {
        valueLabel.Text = "0";
        valueLabel.Dock = DockStyle.Top;
        valueLabel.Height = 40;
        valueLabel.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        valueLabel.ForeColor = UiKit.TextDark;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.Padding = new Padding(2, 0, 0, 0);

        var captionLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F),
            ForeColor = UiKit.TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 0, 0)
        };

        captionLabel.Text = caption;

        var cardPanel = new Panel
        {
            Width = 230,
            Height = 106,
            BackColor = Color.White,
            Padding = new Padding(18, 16, 18, 16),
            Margin = new Padding(0, 0, 16, 0)
        };

        cardPanel.Controls.Add(captionLabel);
        cardPanel.Controls.Add(valueLabel);

        return cardPanel;
    }

    private void LoadDashboardData()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            _clientsCountLabel.Text = ReadScalar(connection, "SELECT COUNT(*) FROM clients;").ToString();
            _devicesCountLabel.Text = ReadScalar(connection, "SELECT COUNT(*) FROM devices;").ToString();
            _activeOrdersCountLabel.Text = ReadScalar(connection, "SELECT COUNT(*) FROM repair_orders WHERE status_id IN (1, 2, 3, 4);").ToString();
            _partsCountLabel.Text = ReadScalar(connection, "SELECT COALESCE(SUM(quantity), 0) FROM parts;").ToString();

            using var command = connection.CreateCommand();

            command.CommandText = """
SELECT
    ro.repair_order_id AS "№",
    c.full_name AS "Клиент",
    d.manufacturer || ' ' || d.model AS "Устройство",
    rs.status_name AS "Статус",
    substr(ro.created_at, 1, 10) AS "Создана"
FROM repair_orders AS ro
INNER JOIN clients AS c ON c.client_id = ro.client_id
INNER JOIN devices AS d ON d.device_id = ro.device_id
INNER JOIN repair_statuses AS rs ON rs.repair_status_id = ro.status_id
ORDER BY ro.repair_order_id DESC
LIMIT 5;
""";

            var table = new DataTable();
            table.Load(command.ExecuteReader());

            _recentOrdersGrid.DataSource = table;

            var hasRows = table.Rows.Count > 0;

            _recentOrdersGrid.Visible = hasRows;
            _noOrdersLabel.Visible = !hasRows;

            if (hasRows)
            {
                var numberColumn = _recentOrdersGrid.Columns["№"];
                if (numberColumn is not null)
                {
                    numberColumn.FillWeight = 14F;
                }

                var createdColumn = _recentOrdersGrid.Columns["Создана"];
                if (createdColumn is not null)
                {
                    createdColumn.FillWeight = 24F;
                }

                var statusColumn = _recentOrdersGrid.Columns["Статус"];
                if (statusColumn is not null)
                {
                    statusColumn.FillWeight = 26F;
                }
            }
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

    private static long ReadScalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = command.ExecuteScalar();
        return Convert.ToInt64(result);
    }

    private void OpenSectionForm(Func<UserSession, Form> sectionFactory)
    {
        using var sectionForm = sectionFactory(_userSession);
        sectionForm.ShowDialog(this);
        LoadDashboardData();
    }

    private void RequestLogout()
    {
        var result = MessageBox.Show(
            this,
            "Выйти из системы?",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            LogoutRequested = true;
            Close();
        }
    }
}