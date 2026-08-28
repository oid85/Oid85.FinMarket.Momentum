namespace Oid85.FinMarket.Algo.Application.Interfaces.Repositories
{
    public interface IParameterRepository
    {
        Task<string?> GetParameterValueAsync(string name);
        Task SetParameterValueAsync(string name, string value);
    }
}
