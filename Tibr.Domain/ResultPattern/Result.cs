using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Domain.ResultPattern;

namespace Tibr.Domain.ResultPattern
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? ErrorMessage { get; }

        protected Result(bool isSuccess, string? errorMessage)
        {

            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static Result Success() => new(true, null);
        public static Result Failure(string errorMessage) => new(false, errorMessage);

     
    }
}

public class Result<T> : Result
{
    public T? Data { get; }

   
    private Result(T? data, bool isSuccess, string? errorMessage)
        : base(isSuccess, errorMessage)
    {
        Data = data;
    }

    
    public static Result<T> Success(T data) => new(data, true, null);

   
    public  static Result<T> Failure(string errorMessage) => new(default, false, errorMessage);

    public static implicit operator Result<T>(T data) => Success(data);
}
