using Microsoft.AspNetCore.Mvc;
using Oid85.FinMarket.Algo.Core;
using Oid85.FinMarket.Algo.Core.Exceptions;

namespace Oid85.FinMarket.Algo.WebHost.Controller.Base;

[ApiController]
public abstract class BaseController : ControllerBase
{
    [ApiExplorerSettings(IgnoreApi = true)]
    protected async Task<IActionResult> GetResponseAsync<TData, TResult>(Func<Task> mainLogic) =>
        await RunBusinessLogic<TData, BaseResponse<TData>>(async () =>
        {
            await mainLogic();
            return Ok(new BaseResponse<TData>());
        });

    [ApiExplorerSettings(IgnoreApi = true)]
    protected async Task<IActionResult> GetResponseAsync<TData, TResult>(
        Func<Task<TData>> mainLogic, Func<TData, TResult> mapping)
        where TResult : BaseResponse<TData>, new() =>
        await RunBusinessLogic<TData, TResult>(async () =>
        {
            var data = await mainLogic();
            return Ok(mapping(data));
        });

    private async Task<IActionResult> RunBusinessLogic<TData, TResult>(Func<Task<IActionResult>> logic)
        where TResult : BaseResponse<TData>, new()
    {
        try
        {
            return await logic();
        }

        catch (BadHttpRequestException exception)
        {
            return BadRequest(new TResult
            {
                Error = new ResponseError
                {
                    Code = StatusCodes.Status400BadRequest.ToString(),
                    Message = exception.Message
                }
            });
        }

        catch (NotFoundException exception)
        {
            return NotFound(new TResult
            {
                Error = new ResponseError
                {
                    Code = exception.Code,
                    Message = exception.Message
                }
            });
        }

        catch (CustomBusinessException exception)
        {
            return BadRequest(new TResult
            {
                Error = new ResponseError
                {
                    Code = exception.Code,
                    Message = exception.Message
                }
            });
        }

        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new TResult
                {
                    Error = new ResponseError
                    {
                        Code = StatusCodes.Status500InternalServerError.ToString(),
                        Message = exception.Message
                    }
                });
        }
    }
}