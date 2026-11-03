namespace Market.Application.Modules.Clinic.WorkingHours.Commands.Update;

public sealed class UpdateWorkingHoursCommandHandler(IAppDbContext ctx)
    :IRequestHandler<UpdateWorkingHoursCommand, Unit>
{
    public async Task<Unit> Handle(UpdateWorkingHoursCommand command, CancellationToken ct)
    {
        var workingHours = await ctx.WorkingHours.FirstOrDefaultAsync(x => x.Id == command.Id, ct);

        if(workingHours is null)
            throw new MarketNotFoundException($"Radno vrijeme sa Id-em {command.Id} nije pronađeno");

        workingHours.DayOfWeek = command.DayOfWeek;
        workingHours.StartTime = command.StartTime;
        workingHours.EndTime = command.EndTime;

        await ctx.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
