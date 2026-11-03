namespace Market.Application.Modules.Clinic.WorkingHours.Commands.Delete;

public sealed class DeleteWorkingHoursCommandHandler(IAppDbContext ctx) : IRequestHandler<DeleteWorkingHoursCommand, Unit>
{
    public async Task<Unit> Handle (DeleteWorkingHoursCommand command, CancellationToken ct)
    {
        var workingHours = await ctx.WorkingHours.FirstOrDefaultAsync(x => x.Id == command.Id, ct);

        if (workingHours is null)
            throw new MarketNotFoundException($"Radno vrijeme sa Id-em {command.Id} nije pronađeno");

        ctx.WorkingHours.Remove(workingHours);

        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
