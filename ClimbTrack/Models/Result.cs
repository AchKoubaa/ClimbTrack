namespace ClimbTrack.Models
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string ErrorMessage { get; }
        public Exception Exception { get; }
        public ErrorSeverity Severity { get; }

        private Result(bool isSuccess, T value, string errorMessage, Exception exception, ErrorSeverity severity)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            Exception = exception;
            Severity = severity;
        }

        public static Result<T> Success(T value) =>
            new Result<T>(true, value, null, null, ErrorSeverity.Info);

        public static Result<T> Failure(string errorMessage, ErrorSeverity severity = ErrorSeverity.Error) =>
            new Result<T>(false, default, errorMessage, null, severity);

        public static Result<T> Failure(Exception ex, ErrorSeverity severity = ErrorSeverity.Error) =>
            new Result<T>(false, default, ex.Message, ex, severity);
    }

    // Non-generic version for operations that don't return a value
    public class Result
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public Exception Exception { get; }
        public ErrorSeverity Severity { get; }

        private Result(bool isSuccess, string errorMessage, Exception exception, ErrorSeverity severity)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Exception = exception;
            Severity = severity;
        }

        public static Result Success() =>
            new Result(true, null, null, ErrorSeverity.Info);

        public static Result Failure(string errorMessage, ErrorSeverity severity = ErrorSeverity.Error) =>
            new Result(false, errorMessage, null, severity);

        public static Result Failure(Exception ex, ErrorSeverity severity = ErrorSeverity.Error) =>
            new Result(false, ex.Message, ex, severity);
    }
}