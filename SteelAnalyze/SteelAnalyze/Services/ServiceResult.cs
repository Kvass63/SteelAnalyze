namespace SteelAnalyze.Services
{
    public enum ServiceErrorKind
    {
        NotFound,
        Conflict,
        InvalidInput,
        InvalidCredentials,
        ModelUnavailable
    }

    public record ServiceError(ServiceErrorKind Kind, string Message)
    {
        public static ServiceError NotFound(string message) => new(ServiceErrorKind.NotFound, message);
        public static ServiceError Conflict(string message) => new(ServiceErrorKind.Conflict, message);
        public static ServiceError InvalidInput(string message) => new(ServiceErrorKind.InvalidInput, message);
        public static ServiceError InvalidCredentials() => new(ServiceErrorKind.InvalidCredentials, "Неверный логин или пароль");
        public static ServiceError ModelUnavailable() => new(ServiceErrorKind.ModelUnavailable, "Модель распознавания не загружена");
    }

    public class ServiceResult
    {
        public static readonly ServiceResult Success = new(null);

        public ServiceError? Error { get; }

        private ServiceResult(ServiceError? error)
        {
            Error = error;
        }

        public static implicit operator ServiceResult(ServiceError error) => new(error);
    }

    public class ServiceResult<T>
    {
        public T? Value { get; }
        public ServiceError? Error { get; }

        private ServiceResult(T? value, ServiceError? error)
        {
            Value = value;
            Error = error;
        }

        public static implicit operator ServiceResult<T>(T value) => new(value, null);
        public static implicit operator ServiceResult<T>(ServiceError error) => new(default, error);
    }
}
