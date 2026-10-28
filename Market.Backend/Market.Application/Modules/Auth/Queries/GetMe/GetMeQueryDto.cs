namespace Market.Application.Modules.Auth.Queries.GetMe;

public sealed class GetMeQueryDto
{
    public required int Id { get; init; }
    public required string Email { get; init; }
    public required string Firstname { get; init; }
    public required string Lastname { get; init; }
    public required string? Address { get; init; }
    public required string? City { get; init; }

    // This is used for authorization, so we want to serialize it as a string in JSON, not as an integer.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Role Role { get; init; }
}
