namespace Application.Models.Responses;

public class BaseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
}
