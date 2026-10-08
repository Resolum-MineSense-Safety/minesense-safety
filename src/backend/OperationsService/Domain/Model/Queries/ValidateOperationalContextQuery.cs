namespace OperationsService.Domain.Model.Queries;

public record ValidateOperationalContextQuery(Guid OperatorId, string VehicleCode, DateOnly Date);
