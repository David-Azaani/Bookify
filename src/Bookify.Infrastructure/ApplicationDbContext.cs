using Bookify.Application.Abstractions.Clock;
using Bookify.Application.Exceptions;
using Bookify.Domain.Abstractions;
using Bookify.Infrastructure.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Bookify.Infrastructure;

public sealed class ApplicationDbContext : DbContext, IUnitOfWork




{

    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
    };

    private readonly IDateTimeProvider _dateTimeProvider;

    //private readonly IPublisher _publisher;


    public ApplicationDbContext(DbContextOptions options, IDateTimeProvider dateTimeProvider) : base(options)
    {
        //_publisher = publisher;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)



    {

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            //await PublishDomainEventsAsync();
            AddDomainEventsAsOutboxMessages();
            var result = await base.SaveChangesAsync(cancellationToken);


            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Concurrency exception occurred.", ex);
        }
    }
    private void AddDomainEventsAsOutboxMessages()
    {
        #region Old
        // Before Outbox
        //var domainEvents = ChangeTracker
        //    .Entries<Entity>()
        //    .Select(entry => entry.Entity)
        //    .SelectMany(entity =>
        //    {
        //        var domainEvents = entity.GetDomainEvents(); //Get events

        //        entity.ClearDomainEvents(); //Clearing from source

        //        return domainEvents;
        //    })
        //    .ToList();
        //foreach (var domainEvent in domainEvents) // publishing one by one
        //{
        //    await _publisher.Publish(domainEvent);
        //} 
        #endregion

        var outboxMessages = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                var domainEvents = entity.GetDomainEvents(); //Get events

                entity.ClearDomainEvents(); //Clearing from source

                return domainEvents;
            }).Select(domainEvent => new OutboxMessage(
                Guid.NewGuid(), 
                _dateTimeProvider.UtcNow,
                domainEvent.GetType().Name,
                JsonConvert.SerializeObject(domainEvent, JsonSerializerSettings)
                ))

            .ToList();
        // we have access to the add range method as we are in db context
        AddRange(outboxMessages);

    }
}