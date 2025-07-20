namespace ClaimsProcessor.Services.Messaging;

public interface IMessagePublisher
{
    Task PublishClaimEvent(string eventType, int claimId, int patientId);
    Task PublishMessage<T>(string routingKey, T message) where T : class;
}