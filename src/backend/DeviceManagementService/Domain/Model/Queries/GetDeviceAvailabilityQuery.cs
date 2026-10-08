using DeviceManagementService.Domain.Model.ValueObjects;

namespace DeviceManagementService.Domain.Model.Queries;

// Sprint 1 · T12 (Farid Coronel): supervisor availability view (GET /api/v1/devices?status=).
// Remaining: supervisor view in src/frontend.

/// <summary>All registered devices, optionally filtered by status.</summary>
public record GetDeviceAvailabilityQuery(DeviceStatus? Status = null);
