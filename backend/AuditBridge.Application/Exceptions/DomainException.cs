namespace AuditBridge.Application.Exceptions;

public class DomainException(string message) : Exception(message);

public class NotFoundException(string entity, object id)
    : DomainException($"{entity} with id '{id}' was not found.");

public class UnauthorizedException(string message = "Access denied.")
    : DomainException(message);

public class ValidationException(string field, string message)
    : DomainException($"Validation failed for '{field}': {message}");
