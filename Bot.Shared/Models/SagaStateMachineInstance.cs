// Bot.Core/Orchestrators/BotState.cs

using MassTransit;

namespace Bot.Shared.Models
{
    public class BotState : SagaStateMachineInstance
    {
        /* Required */
        public Guid   CorrelationId  { get; set; }
        public string CurrentState   { get; set; } = default!;

        /* Signup stack */
        public bool   KycApproved    { get; set; }
        public bool   BankLinked     { get; set; }
        public bool   PinSet         { get; set; }
        public bool   PinValidated   { get; set; }

        /* Ops context */
        public Guid?  ActiveBillId        { get; set; }
        public Guid?  ActiveRecurringId   { get; set; }
        public Guid?  ActiveGoalId        { get; set; }
        public Guid?  LastTransactionId   { get; set; }

        public string? LastFailureReason  { get; set; }

        /* Audit */
        public DateTime CreatedUtc  { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc  { get; set; } = DateTime.UtcNow;
        
        
        public string? TempName { get; set; }
        public string? TempNIN  { get; set; }
        public string? TempBVN  { get; set; }
        public Guid     SessionId { get; set; }      // populated on first inbound webhook
        public string?  PhoneNumber { get; set; }    // set once at beginning

        public byte[]? RowVersion { get; set; }
    }
}