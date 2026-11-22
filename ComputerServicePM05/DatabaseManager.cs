using System.IO;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public static class DatabaseManager
{
    private static readonly string DatabasePath = Path.Combine(AppContext.BaseDirectory, "Computer_service.db");

    public static string ConnectionString { get; } = $"Data Source={DatabasePath};Cache=Shared;Foreign Keys=True";

    public static SqliteConnection CreateConnection() => new(ConnectionString);

    public static void InitializeDatabase()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = InitializationScript;
        command.ExecuteNonQuery();
    }

    private const string InitializationScript = """
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS roles (
    role_id INTEGER PRIMARY KEY AUTOINCREMENT,
    role_name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS users (
    user_id INTEGER PRIMARY KEY AUTOINCREMENT,
    login TEXT NOT NULL UNIQUE,
    password TEXT NOT NULL,
    full_name TEXT NOT NULL,
    role_id INTEGER NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (role_id) REFERENCES roles(role_id)
);

CREATE TABLE IF NOT EXISTS clients (
    client_id INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name TEXT NOT NULL,
    phone TEXT NOT NULL,
    email TEXT
);

CREATE TABLE IF NOT EXISTS device_categories (
    device_category_id INTEGER PRIMARY KEY AUTOINCREMENT,
    category_name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS devices (
    device_id INTEGER PRIMARY KEY AUTOINCREMENT,
    device_category_id INTEGER NOT NULL,
    manufacturer TEXT NOT NULL,
    model TEXT NOT NULL,
    serial_number TEXT,
    FOREIGN KEY (device_category_id) REFERENCES device_categories(device_category_id)
);

CREATE TABLE IF NOT EXISTS repair_statuses (
    repair_status_id INTEGER PRIMARY KEY AUTOINCREMENT,
    status_name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS repair_orders (
    repair_order_id INTEGER PRIMARY KEY AUTOINCREMENT,
    client_id INTEGER NOT NULL,
    device_id INTEGER NOT NULL,
    assigned_user_id INTEGER,
    reported_defect TEXT NOT NULL,
    status_id INTEGER NOT NULL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    closed_at TEXT,
    total_cost REAL NOT NULL DEFAULT 0,
    notes TEXT,
    FOREIGN KEY (client_id) REFERENCES clients(client_id),
    FOREIGN KEY (device_id) REFERENCES devices(device_id),
    FOREIGN KEY (assigned_user_id) REFERENCES users(user_id),
    FOREIGN KEY (status_id) REFERENCES repair_statuses(repair_status_id)
);

CREATE TABLE IF NOT EXISTS parts (
    part_id INTEGER PRIMARY KEY AUTOINCREMENT,
    article TEXT NOT NULL UNIQUE,
    part_name TEXT NOT NULL,
    price REAL NOT NULL CHECK(price >= 0),
    quantity INTEGER NOT NULL DEFAULT 0 CHECK(quantity >= 0)
);

CREATE TABLE IF NOT EXISTS repair_order_parts (
    repair_order_part_id INTEGER PRIMARY KEY AUTOINCREMENT,
    repair_order_id INTEGER NOT NULL,
    part_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL CHECK(quantity > 0),
    price_at_use REAL NOT NULL CHECK(price_at_use >= 0),
    FOREIGN KEY (repair_order_id) REFERENCES repair_orders(repair_order_id),
    FOREIGN KEY (part_id) REFERENCES parts(part_id),
    UNIQUE(repair_order_id, part_id)
);

INSERT OR IGNORE INTO roles (role_id, role_name) VALUES
(1, 'Администратор'),
(2, 'Пользователь');

INSERT OR IGNORE INTO repair_statuses (repair_status_id, status_name) VALUES
(1, 'Новая'),
(2, 'Диагностика'),
(3, 'В работе'),
(4, 'Ожидает запчасть'),
(5, 'Готово'),
(6, 'Выдано'),
(7, 'Отменено');

INSERT OR IGNORE INTO device_categories (device_category_id, category_name) VALUES
(1, 'Ноутбук'),
(2, 'ПК'),
(3, 'Монитор'),
(4, 'Принтер'),
(5, 'Смартфон'),
(6, 'Другое');

INSERT OR IGNORE INTO users (login, password, full_name, role_id, is_active) VALUES
('admin', 'admin', 'Администратор системы', 1, 1),
('user', 'user', 'Сотрудник сервисного центра', 2, 1);
""";
}