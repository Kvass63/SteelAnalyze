namespace SteelAnalyze.Services
{
    public enum ServiceErrorKind
    {
        NotFound,           // Запись не найдена -> 404
        Conflict,           // Операция противоречит данным в базе: повтор, запись используется, не то состояние -> 409
        InvalidInput,       // Неверные входные данные -> 400
        InvalidCredentials, // Неверный логин или пароль -> 401
        ModelUnavailable    // Файл модели не найден, распознавание недоступно -> 503
    }

    // Почему операция сервиса не выполнена. В ответ HTTP ошибку превращает ApiControllerBase
    public record ServiceError(ServiceErrorKind Kind, string Message)
    {
        public static ServiceError NotFound(string message) => new(ServiceErrorKind.NotFound, message);
        public static ServiceError Conflict(string message) => new(ServiceErrorKind.Conflict, message);
        public static ServiceError InvalidInput(string message) => new(ServiceErrorKind.InvalidInput, message);
        public static ServiceError InvalidCredentials() => new(ServiceErrorKind.InvalidCredentials, "Неверный логин или пароль");
        public static ServiceError ModelUnavailable() => new(ServiceErrorKind.ModelUnavailable, "Модель распознавания не загружена");
    }

    // Результат операции, которая ничего не возвращает: успех или ошибка
    public class ServiceResult
    {
        public static readonly ServiceResult Success = new(null);

        public ServiceError? Error { get; }

        private ServiceResult(ServiceError? error)
        {
            Error = error;
        }

        // Позволяет писать в сервисе просто: return ServiceError.NotFound(...);
        public static implicit operator ServiceResult(ServiceError error) => new(error);
    }

    // Результат операции, которая возвращает данные: данные или ошибка
    public class ServiceResult<T>
    {
        public T? Value { get; }
        public ServiceError? Error { get; }

        private ServiceResult(T? value, ServiceError? error)
        {
            Value = value;
            Error = error;
        }

        // Позволяют писать в сервисе просто: return defectClass; или return ServiceError.NotFound(...);
        public static implicit operator ServiceResult<T>(T value) => new(value, null);
        public static implicit operator ServiceResult<T>(ServiceError error) => new(default, error);
    }
}
