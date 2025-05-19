using System;

namespace NeoServiceLayer.Shared.Models
{
    public class ApiResponse<T>
    {
        public ApiResponse()
        {
            Success = false;
            Error = new ApiError();
        }

        public bool Success { get; set; }
        public T Data { get; set; }
        public ApiError Error { get; set; }
    }

    public class ApiError
    {
        public ApiError()
        {
            Code = string.Empty;
            Message = string.Empty;
            Details = string.Empty;
        }

        public string Code { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }

    public static class ApiErrorCodes
    {
        public const string ValidationError = "validation_error";
        public const string ResourceNotFound = "resource_not_found";
        public const string InternalServerError = "internal_server_error";
        public const string Unauthorized = "unauthorized";
        public const string Forbidden = "forbidden";
        public const string BadRequest = "bad_request";
    }

    public class PaginatedResult<T>
    {
        public PaginatedResult()
        {
            Items = Array.Empty<T>();
            TotalCount = 0;
            Page = 1;
            PageSize = 10;
            TotalPages = 0;
        }

        public T[] Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
