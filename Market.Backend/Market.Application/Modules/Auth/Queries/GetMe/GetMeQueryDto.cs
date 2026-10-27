namespace Market.Application.Modules.Auth.Queries.GetMe;

public sealed class GetMeQueryDto
{
    public required int Id { get; init; }
    public required string Email { get; init; }
    public required string Firstname { get; init; }
    public required string Lastname { get; init; }
    public required string? Address { get; init; }
    public required string? City { get; init; }
    public required bool IsAdmin { get; init; }
    public required bool IsManager { get; init; }
    public required bool IsEmployee { get; init; }
}
