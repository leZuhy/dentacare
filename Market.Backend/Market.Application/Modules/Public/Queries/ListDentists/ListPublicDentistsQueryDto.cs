namespace Market.Application.Modules.Public.Queries.ListDentists;

/// <summary>
/// Public data of a dentist. No email: it is also the login, and the public doesn't need it.
/// </summary>
public sealed class ListPublicDentistsQueryDto
{
    public required int Id { get; init; }
    public required string Firstname { get; init; }
    public required string Lastname { get; init; }
    public required int SpecializationId { get; init; }
    public required string SpecializationName { get; init; }
    public required string? Bio { get; init; }
}