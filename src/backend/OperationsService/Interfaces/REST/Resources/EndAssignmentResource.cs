using System.Text.Json.Serialization;

namespace OperationsService.Interfaces.REST.Resources;

public record EndAssignmentResource([property: JsonRequired] DateOnly EndDate);
