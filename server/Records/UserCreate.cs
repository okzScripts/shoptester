namespace server.Records;

public record UserCreate(string Username, string Email, string Password, int RoleId);