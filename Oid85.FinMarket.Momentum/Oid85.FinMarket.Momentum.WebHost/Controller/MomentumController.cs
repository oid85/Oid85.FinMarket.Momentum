using Microsoft.AspNetCore.Mvc;
using Oid85.FinMarket.Momentum.Application.Interfaces.Services;
using Oid85.FinMarket.Momentum.Core;
using Oid85.FinMarket.Momentum.Core.Requests;
using Oid85.FinMarket.Momentum.Core.Responses;
using Oid85.FinMarket.Momentum.WebHost.Controller.Base;

namespace Oid85.FinMarket.Momentum.WebHost.Controller;

/// <summary>
/// Моментум
/// </summary>
[Route("api/momentum")]
[ApiController]
public class MomentumController(
    IMomentumService momentumService)
    : BaseController
{
    /// <summary>
    /// Мониторинг
    /// </summary>
    [HttpPost("monitor")]
    [ProducesResponseType(typeof(BaseResponse<MonitorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<MonitorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<MonitorResponse>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> MonitorAsync(
        [FromBody] MonitorRequest request) =>
        GetResponseAsync(
            () => momentumService.MonitorAsync(request),
            result => new BaseResponse<MonitorResponse> { Result = result });   
}