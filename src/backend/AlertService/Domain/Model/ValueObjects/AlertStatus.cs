namespace AlertService.Domain.Model.ValueObjects;

public enum AlertStatus
{
    Issued,
    Acknowledged,
    Escalated,

    /// <summary>
    /// The cabin warned the operator by itself and the Edge reported it later. There is nothing left
    /// to deliver, acknowledge or escalate, so it never enters the acknowledgement flow.
    /// </summary>
    RecordedFromEdge
}
