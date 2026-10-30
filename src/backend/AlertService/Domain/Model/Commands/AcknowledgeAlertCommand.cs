namespace AlertService.Domain.Model.Commands;

public record AcknowledgeAlertCommand(Guid AlertId);
