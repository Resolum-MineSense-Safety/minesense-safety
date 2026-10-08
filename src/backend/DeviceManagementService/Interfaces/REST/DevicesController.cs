using System.Net.Mime;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Services;
using DeviceManagementService.Interfaces.REST.Resources;
using DeviceManagementService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementService.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class DevicesController(
    IMonitoringDeviceCommandService deviceCommandService,
    IMonitoringDeviceQueryService deviceQueryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceResource resource)
    {
        var command = RegisterDeviceCommandFromResourceAssembler.ToCommandFromResource(resource);
        var device = await deviceCommandService.Handle(command);
        var deviceResource = DeviceResourceFromEntityAssembler.ToResourceFromEntity(device);
        return CreatedAtAction(nameof(GetDeviceById), new { deviceId = device.Id }, deviceResource);
    }

    [HttpGet("{deviceId:guid}")]
    public async Task<IActionResult> GetDeviceById(Guid deviceId)
    {
        var device = await deviceQueryService.Handle(new GetDeviceByIdQuery(deviceId));
        if (device is null) return NotFound();
        return Ok(DeviceResourceFromEntityAssembler.ToResourceFromEntity(device));
    }

    /// <summary>
    /// Devices assigned to an operator (?operatorId=) or, without it, the availability
    /// of every device optionally filtered by status (?status=).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDevices([FromQuery] Guid? operatorId, [FromQuery] string? status)
    {
        if (operatorId.HasValue)
        {
            var assigned = await deviceQueryService.Handle(new GetDevicesByOperatorIdQuery(operatorId.Value));
            return Ok(assigned.Select(DeviceResourceFromEntityAssembler.ToResourceFromEntity));
        }

        // Sprint 1 · T12 (Farid Coronel): supervisor availability view; remaining: frontend view.
        var statusFilter = DeviceStatusFromQueryAssembler.ToStatusFromQuery(status);
        var devices = await deviceQueryService.Handle(new GetDeviceAvailabilityQuery(statusFilter));
        return Ok(devices.Select(DeviceResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPut("{deviceId:guid}/assignment")]
    public async Task<IActionResult> AssignDevice(Guid deviceId, [FromBody] AssignDeviceResource resource)
    {
        var device = await deviceCommandService.Handle(
            new AssignDeviceCommand(deviceId, resource.OperatorId, resource.VehicleCode));
        if (device is null) return NotFound();
        return Ok(DeviceResourceFromEntityAssembler.ToResourceFromEntity(device));
    }

    [HttpPost("{deviceId:guid}/heartbeats")]
    public async Task<IActionResult> ReportHeartbeat(Guid deviceId, [FromBody] ReportHeartbeatResource resource)
    {
        var device = await deviceCommandService.Handle(new ReportHeartbeatCommand(deviceId, resource.BatteryLevel));
        if (device is null) return NotFound();
        return Ok(DeviceResourceFromEntityAssembler.ToResourceFromEntity(device));
    }

    [HttpPost("{deviceId:guid}/disconnections")]
    public async Task<IActionResult> ReportDisconnection(Guid deviceId, [FromBody] ReportDisconnectionResource resource)
    {
        var device = await deviceCommandService.Handle(new ReportDisconnectionCommand(deviceId, resource.Reason));
        if (device is null) return NotFound();
        return Ok(DeviceResourceFromEntityAssembler.ToResourceFromEntity(device));
    }

    [HttpPost("{deviceId:guid}/recoveries")]
    public async Task<IActionResult> ReportRecovery(Guid deviceId)
    {
        var device = await deviceCommandService.Handle(new ReportRecoveryCommand(deviceId));
        if (device is null) return NotFound();
        return Ok(DeviceResourceFromEntityAssembler.ToResourceFromEntity(device));
    }
}
