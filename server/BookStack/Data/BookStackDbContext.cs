namespace BookStack.Data;

using System.Linq.Expressions;
using System.Reflection;
using Features.BookListings.Data.Models;
using Features.Books.Data.Models;
using Features.Identity.Data.Models;
using Features.Orders.Data.Models;
using Features.Payments.Data.Models;
using Features.SellerProfiles.Data.Models;
using Features.UserProfile.Data.Models;
using Infrastructure.Services.CurrentUser;
using Infrastructure.Services.DateTimeProvider;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Models.Base;

public class BookStackDbContext(
    DbContextOptions<BookStackDbContext> options,
    ICurrentUserService userService,
    IDateTimeProvider dateTimeProvider) : IdentityDbContext<UserDbModel>(options)
{
    private readonly ICurrentUserService _userService = userService;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public DbSet<BookDbModel> Books { get; init; }

    public DbSet<BookListingDbModel> BookListings { get; init; }

    public DbSet<UserProfileDbModel> Profiles { get; init; }

    public DbSet<SellerProfileDbModel> SellerProfiles { get; init; }

    public DbSet<OrderDbModel> Orders { get; init; }

    public DbSet<OrderItemDbModel> OrderItems { get; init; }

    public DbSet<PaymentDbModel> Payments { get; init; }

    public DbSet<PaymentWebhookEventDbModel> PaymentWebhookEvents { get; init; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.ApplyAuditInfo();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, 
        CancellationToken cancellationToken = default)
    {
        this.ApplyAuditInfo();

        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly());

        this.FilterModels(modelBuilder);
    }

    private void ApplyAuditInfo() 
        => this.ChangeTracker
            .Entries()
            .ToList()
            .ForEach(entry =>
            {
                var utcNow = this._dateTimeProvider.UtcNow;
                var username = this._userService.GetUsername();

                if (entry.State == EntityState.Deleted && 
                    entry.Entity is IDeletableEntity deletableEntity)
                {
                    deletableEntity.DeletedOn = utcNow;
                    deletableEntity.DeletedBy = username;
                    deletableEntity.IsDeleted = true;

                    entry.State = EntityState.Modified;

                    return;
                }

                if (entry.Entity is IDeletableEntity entity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        entity.CreatedOn = utcNow;
                        entity.CreatedBy = username;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        entity.ModifiedOn = utcNow;
                        entity.ModifiedBy = username;
                    }
                }
            });

    private void FilterModels(ModelBuilder modelBuilder)
        => modelBuilder
            .Model
            .GetEntityTypes()
            .ToList()
            .ForEach(entityType =>
            {
                var clrType = entityType.ClrType;
                var filter = BuildFilterExpression(clrType);

                if (filter is not null)
                {
                    modelBuilder.Entity(clrType).HasQueryFilter(filter);
                }
            });

    private static LambdaExpression? BuildFilterExpression(Type entityType)
    {
        if (!typeof(IDeletableEntity).IsAssignableFrom(entityType))
        {
            return null;
        }

        var entityTypeParam = Expression.Parameter(entityType, "e");

        var isDeleted = Expression.Property(
            entityTypeParam,
            nameof(IDeletableEntity.IsDeleted));

        var isNotDeleted = Expression.Equal(
            isDeleted,
            Expression.Constant(false));

        return Expression.Lambda(isNotDeleted, entityTypeParam);
    }
}
