using OperationsService.Domain.Model.ValueObjects;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class OperationalContextResourceFromValidationAssembler
{
    public static OperationalContextResource ToResourceFromValidation(OperationalContextValidation validation) =>
        new(validation.Valid, validation.AssignmentId, validation.Reason);
}
