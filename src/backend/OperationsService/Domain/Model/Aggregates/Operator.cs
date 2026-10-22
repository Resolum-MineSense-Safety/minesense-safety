using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Domain.Model.Aggregates;

/// <summary>
/// Heavy machinery operator of a mining unit (TS-07). The employee code (identificador laboral)
/// is unique across the company and upper-cased; assignments, devices and monitoring sessions
/// refer to the operator by its id.
/// </summary>
public class Operator : AggregateRoot
{
    public string EmployeeCode { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Guid MiningUnitId { get; private set; }
    public OperatorStatus Status { get; private set; }

    public Operator(RegisterOperatorCommand command)
    {
        MandatoryData.EnsurePresent(
            (nameof(EmployeeCode), string.IsNullOrWhiteSpace(command.EmployeeCode)),
            (nameof(FirstName), string.IsNullOrWhiteSpace(command.FirstName)),
            (nameof(LastName), string.IsNullOrWhiteSpace(command.LastName)),
            (nameof(MiningUnitId), command.MiningUnitId == Guid.Empty));

        EmployeeCode = NormalizeCode(command.EmployeeCode);
        FirstName = command.FirstName.Trim();
        LastName = command.LastName.Trim();
        MiningUnitId = command.MiningUnitId;
        Status = OperatorStatus.Active;
    }

    public static string NormalizeCode(string employeeCode) => employeeCode.Trim().ToUpperInvariant();

    public string FullName => $"{FirstName} {LastName}";

    public bool IsActive => Status == OperatorStatus.Active;

    public void Update(UpdateOperatorCommand command)
    {
        MandatoryData.EnsurePresent(
            (nameof(FirstName), string.IsNullOrWhiteSpace(command.FirstName)),
            (nameof(LastName), string.IsNullOrWhiteSpace(command.LastName)),
            (nameof(MiningUnitId), command.MiningUnitId == Guid.Empty));

        FirstName = command.FirstName.Trim();
        LastName = command.LastName.Trim();
        MiningUnitId = command.MiningUnitId;
        Status = command.Status;
    }
}
