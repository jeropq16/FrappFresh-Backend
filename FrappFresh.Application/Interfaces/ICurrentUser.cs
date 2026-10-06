namespace FrappFresh.Application.Interfaces;

// El adaptador HTTP obtiene el ID del JWT validado, nunca del cuerpo de una solicitud.
public interface ICurrentUser
{
    Guid? UserId { get; }
}
