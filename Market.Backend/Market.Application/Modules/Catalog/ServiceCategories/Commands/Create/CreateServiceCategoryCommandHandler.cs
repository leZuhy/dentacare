using Market.Domain.Entities.Catalog;

namespace Market.Application.Modules.Catalog.ServiceCategories.Commands.Create;

public sealed class CreateServiceCategoryCommandHandler(IAppDbContext ctx)
    : IRequestHandler<CreateServiceCategoryCommand, int>
{
    public async Task<int> Handle(CreateServiceCategoryCommand request, CancellationToken ct)
    {
        var category = new ServiceCategoryEntity
        {
            Name = request.Name.Trim()
        };

        ctx.ServiceCategories.Add(category);
        await ctx.SaveChangesAsync(ct);

        return category.Id;
    }
}
