namespace OperationsService.Interfaces.REST.Resources;

public record MiningUnitResource(Guid Id, string BusinessCode, string Name, string Region, IEnumerable<LocationResource> Locations);
