namespace Stock.RealTime.API.Code;

public interface IEndPoint
{
    static abstract void MapEndPoint(IEndpointRouteBuilder endpointRouteBuilder);
}
