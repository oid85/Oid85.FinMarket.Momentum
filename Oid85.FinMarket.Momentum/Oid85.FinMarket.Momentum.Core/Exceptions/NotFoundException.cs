namespace Oid85.FinMarket.Algo.Core.Exceptions;

[Serializable]
public class NotFoundException : Exception
{
    public string Code { get; set; }
        
    public NotFoundException(string code, string message) : base(message)            
    {
        Code = code;
    }

    public NotFoundException(string code, string message, Exception exception) : base(message, exception)
    {
        Code = code;
    }
}