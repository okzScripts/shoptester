namespace server.Records;

public record OrderRead(int Id, string Username, string ProductName, int Quantity, int Price, int Total, DateTime CreatedAt);