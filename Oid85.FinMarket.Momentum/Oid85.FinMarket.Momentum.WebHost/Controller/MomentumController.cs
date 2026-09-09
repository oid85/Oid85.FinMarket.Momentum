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
    /// Мониторинг версии
    /// </summary>
    [HttpPost("monitor/version")]
    [ProducesResponseType(typeof(BaseResponse<MonitorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<MonitorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<MonitorResponse>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> MonitorVersion(
        [FromBody] MonitorRequest request) =>
        GetResponseAsync(
            () => momentumService.MonitorVersionAsync(request),
            result => new BaseResponse<MonitorResponse> { Result = result });

    /// <summary>
    /// Редактировать сумму портфеля
    /// </summary>
    [HttpPost("portfolio/total-sum/edit")]
    [ProducesResponseType(typeof(BaseResponse<EditPortfolioTotalSumResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<EditPortfolioTotalSumResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<EditPortfolioTotalSumResponse>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> EditPortfolioTotalSumAsync(
        [FromBody] EditPortfolioTotalSumRequest request) =>
        GetResponseAsync(
            () => momentumService.EditPortfolioTotalSumAsync(request),
            result => new BaseResponse<EditPortfolioTotalSumResponse> { Result = result });
}