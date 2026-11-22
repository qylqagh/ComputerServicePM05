namespace ComputerServicePM05.Models;

public sealed record UserSession(int UserId, string Login, string FullName, string Role);