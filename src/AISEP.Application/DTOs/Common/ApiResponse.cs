namespace AISEP.Application.DTOs.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public ErrorDetail? Error { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> ErrorResponse(string code, string message, List<FieldError>? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ErrorDetail
            {
                Code = code,
                Message = message,
                Details = details
            }
        };
    }

    // Convenient aliases
    public static ApiResponse<T> Ok(T data, string? message = null) => SuccessResponse(data, message);
    public static ApiResponse<T> Fail(string message) => ErrorResponse("ERROR", message);
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public ErrorDetail? Error { get; set; }

    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    public static ApiResponse ErrorResponse(string code, string message, List<FieldError>? details = null)
    {
        return new ApiResponse
        {
            Success = false,
            Error = new ErrorDetail
            {
                Code = code,
                Message = message,
                Details = details
            }
        };
    }
}

public class ErrorDetail
{
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
    public List<FieldError>? Details { get; set; }
}

public class FieldError
{
    public string Field { get; set; } = null!;
    public string Message { get; set; } = null!;
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public PagingInfo Paging { get; set; } = new();
}

public class PagingInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
}
