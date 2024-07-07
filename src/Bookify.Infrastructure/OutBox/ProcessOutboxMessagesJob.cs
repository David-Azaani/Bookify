using System.Data;
using Bookify.Application.Abstractions.Clock;
using Bookify.Application.Abstractions.Data;
using Bookify.Domain.Abstractions;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;

namespace Bookify.Infrastructure.Outbox;

[DisallowConcurrentExecution] // just one instance of outbox message will be run in given time
internal sealed class ProcessOutboxMessagesJob : IJob


{
    // the same setting to serialize json
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    // required dependencies
    private readonly ISqlConnectionFactory _sqlConnectionFactory; // Using Dapper to query OutBox Messages
    private readonly IPublisher _publisher; // Form Mediator to publish Domain Events
    private readonly IDateTimeProvider _dateTimeProvider; // When
    private readonly OutboxOptions _outboxOptions; // read Option
    private readonly ILogger<ProcessOutboxMessagesJob> _logger; // log operation

    public ProcessOutboxMessagesJob(
        ISqlConnectionFactory sqlConnectionFactory,
        IPublisher publisher,
        IDateTimeProvider dateTimeProvider,
        IOptions<OutboxOptions> outboxOptions,
        ILogger<ProcessOutboxMessagesJob> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _publisher = publisher;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _outboxOptions = outboxOptions.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Beginning to process outbox messages");

        using IDbConnection connection = _sqlConnectionFactory.CreateConnection();// New Database Connection
        using IDbTransaction transaction = connection.BeginTransaction();// open a transaction , we want to do process all outBox Messages in single transaction 

        IReadOnlyList<OutboxMessageResponse> outboxMessages = await GetOutboxMessagesAsync(connection, transaction); // do query

        foreach (OutboxMessageResponse outboxMessage in outboxMessages)
        {
            Exception? exception = null;

            try
            {
                // deserializing the messages
                IDomainEvent domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                    outboxMessage.Content,
                    JsonSerializerSettings)!;

                // publish

                await _publisher.Publish(domainEvent, context.CancellationToken);
            }
            catch (Exception caughtException)
            {
                // log error and identify the specific rows and error
                _logger.LogError(
                    caughtException,
                    "Exception while processing outbox message {MessageId}",
                    outboxMessage.Id);

                exception = caughtException;
            }

            // update outBox Message

            await UpdateOutboxMessageAsync(connection, transaction, outboxMessage, exception);
        }
        // commit database Transaction
        transaction.Commit();
        // after the locke will be removed from the row

        _logger.LogInformation("Completed processing outbox messages");
    }



    private async Task<IReadOnlyList<OutboxMessageResponse>> GetOutboxMessagesAsync(
        IDbConnection connection,
        IDbTransaction transaction)
    {
        // Note : We use Select for update query , which is going to lock any rows there are in the query , 
        // as part of database transaction , until we commit the transaction, this way we make sure this operation will be run once and
        // apply on our where condition
        // if there are some transactions or  multiple instances of  background job running these rows will be locked
        // and concurrent process of job wont be able to do on those rows.
        // and it's really useful because we want to do this operation once.
        string sql = $"""
                      SELECT id, content
                      FROM outbox_messages
                      WHERE processed_on_utc IS NULL
                      ORDER BY occurred_on_utc
                      LIMIT {_outboxOptions.BatchSize}
                      FOR UPDATE
                      """;

        // we add this FOR UPDATE to make sure these records will be locked

        IEnumerable<OutboxMessageResponse> outboxMessages = await connection.QueryAsync<OutboxMessageResponse>(
            sql,
            transaction: transaction);

        return outboxMessages.ToList();
    }

    private async Task UpdateOutboxMessageAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        OutboxMessageResponse outboxMessage,
        Exception? exception)
    {
        const string sql = @"
            UPDATE outbox_messages
            SET processed_on_utc = @ProcessedOnUtc,
                error = @Error
            WHERE id = @Id";

        await connection.ExecuteAsync(
            sql,
            new
            {
                outboxMessage.Id,
                ProcessedOnUtc = _dateTimeProvider.UtcNow,
                Error = exception?.ToString()
            },
            transaction: transaction);
    }

    internal sealed record OutboxMessageResponse(Guid Id, string Content); // the response type
}
