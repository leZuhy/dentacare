using Microsoft.AspNetCore.Mvc.Filters;

namespace Market.API.Filters;

/// <summary>
/// An action that returns <c>Task</c> (no value), e.g. <c>public async Task Delete(int id, ...)</c>,
/// produces an <see cref="EmptyResult"/>, which ASP.NET Core sends as "200 OK" with an empty body.
/// This filter turns it into "204 No Content", the usual status for PUT/DELETE without a response body,
/// so the comment "no return -> 204 No Content" in the controllers holds for every action.
/// </summary>
public sealed class EmptyResultToNoContentFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is EmptyResult)
            context.Result = new NoContentResult();
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
