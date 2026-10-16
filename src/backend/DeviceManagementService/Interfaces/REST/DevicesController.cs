using System.Net.Mime;
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
    IDeviceCommandService deviceCommandService,
    IDeviceQueryService deviceQueryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(DeviceResource), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterDevice(RegisterDeviceResource resource)
    {
        var device = await deviceCommandService.Handle(
            DeviceCommandFromResourceAssembler.ToRegisterCommand(resource));
        return CreatedAtAction(nameof(GetDeviceStatus), new { deviceId = device.Id },
            DeviceResourceFromEntityAssembler.ToResourceFromEntity(device));
    }

    [HttpPut("{deviceId:guid}/assignment")]
    [ProducesResponseType(typeof(DeviceResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignDevice(Guid deviceId, AssignDeviceResource resource)
    {
        var device = await deviceCommandService.Handle(
            DeviceCommandFromResourceAssembler.ToAssignCommand(deviceId, resource));
        if (device is null) return NotFound();
        return Ok(DeviceResourceFromEntityAssembler.ToResourceFromEntity(device));
    }

    [HttpPost("{deviceId:guid}/status")]
    [ProducesResponseType(typeof(DeviceResource), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportStatus(Guid deviceId, ReportDeviceStatusResource resource)
    {
        var device = await deviceCommandService.Handle(
            DeviceCommandFromResourceAssembler.ToReportStatusCommand(deviceId, resource));
        if (device is null) return NotFound();
        return Accepted(DeviceResourceFromEntityAssembler.ToResourceFromEntity(device));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DeviceResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevices()
    {
        var devices = await deviceQueryService.Handle(new GetDevicesQuery());
        return Ok(devices.Select(DeviceResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{deviceId:guid}/status")]
    [ProducesResponseType(typeof(DeviceResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceStatus(Guid deviceId)
    {
        var device = await deviceQueryService.Handle(new GetDeviceByIdQuery(deviceId));
        if (device is null) return NotFound();
        return Ok(DeviceResourceFromEntityAssembler.ToResourceFromEntity(device));
    }
}
