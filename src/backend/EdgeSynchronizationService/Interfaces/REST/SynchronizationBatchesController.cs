using System.Net.Mime;
using EdgeSynchronizationService.Domain.Model.Queries;
using EdgeSynchronizationService.Domain.Services;
using EdgeSynchronizationService.Interfaces.REST.Resources;
using EdgeSynchronizationService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace EdgeSynchronizationService.Interfaces.REST;

[ApiController]
[Route("api/v1/synchronization-batches")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class SynchronizationBatchesController(
    ISynchronizationCommandService commandService,
    ISynchronizationQueryService queryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SynchronizationBatchResource), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(SynchronizationBatchResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitBatch(SubmitSynchronizationBatchResource resource)
    {
        var (batch, isDuplicate) = await commandService.Handle(
            SynchronizationBatchAssembler.ToCommand(resource));
        var response = SynchronizationBatchAssembler.ToResource(batch);
        return isDuplicate ? Ok(response) : AcceptedAtAction(
            nameof(GetBatch), new { batchId = batch.BatchId }, response);
    }

    [HttpGet("{batchId:guid}")]
    [ProducesResponseType(typeof(SynchronizationBatchResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatch(Guid batchId)
    {
        var batch = await queryService.Handle(new GetSynchronizationBatchQuery(batchId));
        return batch is null
            ? NotFound()
            : Ok(SynchronizationBatchAssembler.ToResource(batch));
    }
}
