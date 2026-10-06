namespace FrappFresh.Application.Exceptions;

public class UnauthorizedException(string message = "Debes iniciar sesión.") : Exception(message);
