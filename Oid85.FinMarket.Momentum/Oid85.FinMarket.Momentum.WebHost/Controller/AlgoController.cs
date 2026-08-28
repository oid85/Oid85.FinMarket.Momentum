using Microsoft.AspNetCore.Mvc;
using Oid85.FinMarket.Algo.Application.Interfaces.Services;
using Oid85.FinMarket.Algo.Core;
using Oid85.FinMarket.Algo.Core.Requests;
using Oid85.FinMarket.Algo.Core.Responses;
using Oid85.FinMarket.Algo.WebHost.Controller.Base;

namespace Oid85.FinMarket.Algo.WebHost.Controller;

/// <summary>
/// Алго
/// </summary>
[Route("api/algo")]
[ApiController]
public class AlgoController(
    IAlgoService algoService)
    : BaseController
{
    /// <summary>
    /// Мониторинг
    /// </summary>
    [HttpPost("portfolio/monitor")]
    [ProducesResponseType(typeof(BaseResponse<MonitorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<MonitorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<MonitorResponse>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> MonitorAsync(
        [FromBody] MonitorRequest request) =>
        GetResponseAsync(
            () => algoService.MonitorAsync(request),
            result => new BaseResponse<MonitorResponse> { Result = result });

    /// <summary>
    /// Список портфелей
    /// </summary>
    [HttpPost("portfolio/list")]
    [ProducesResponseType(typeof(BaseResponse<PortfolioListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<PortfolioListResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<PortfolioListResponse>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> PortfolioListAsync(
        [FromBody] PortfolioListRequest request) =>
        GetResponseAsync(
            () => algoService.PortfolioListAsync(request),
            result => new BaseResponse<PortfolioListResponse> { Result = result });

    /// <summary>
    /// Список стратегий портфеля
    /// </summary>
    [HttpPost("portfolio/strategy/list")]
    [ProducesResponseType(typeof(BaseResponse<StrategyListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<StrategyListResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<StrategyListResponse>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> StrategyListAsync(
        [FromBody] StrategyListRequest request) =>
        GetResponseAsync(
            () => algoService.StrategyListAsync(request),
            result => new BaseResponse<StrategyListResponse> { Result = result });

    /// <summary>
    /// Получить сумму портфеля
    /// </summary>
    [HttpPost("portfolio/total-sum/get")]
    [ProducesResponseType(typeof(BaseResponse<GetPortfolioTotalSumResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<GetPortfolioTotalSumResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<GetPortfolioTotalSumResponse>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPortfolioTotalSumAsync(
        [FromBody] GetPortfolioTotalSumRequest request) =>
        GetResponseAsync(
            () => algoService.GetPortfolioTotalSumAsync(request),
            result => new BaseResponse<GetPortfolioTotalSumResponse> { Result = result });

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
            () => algoService.EditPortfolioTotalSumAsync(request),
            result => new BaseResponse<EditPortfolioTotalSumResponse> { Result = result });    
}