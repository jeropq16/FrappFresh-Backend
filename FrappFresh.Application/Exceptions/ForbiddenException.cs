namespace FrappFresh.Application.Exceptions;

public class ForbiddenException(string message = "No tienes permisos para esta operación.") : Exception(message);
