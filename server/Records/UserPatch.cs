namespace server.Records;

public record UserPatch(string Username, string Email, string Password, int? RoleId);