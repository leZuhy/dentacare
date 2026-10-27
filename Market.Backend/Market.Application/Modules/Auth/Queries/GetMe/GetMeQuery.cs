namespace Market.Application.Modules.Auth.Queries.GetMe;

/// <summary>
/// Profile of the logged-in user (the user comes from the access token, not from the request).
/// </summary>
public sealed class GetMeQuery : IRequest<GetMeQueryDto>
{
}
