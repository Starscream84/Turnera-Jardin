using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Filters;

/// <summary>Traduce las excepciones de negocio de los servicios a respuestas HTTP, para no repetir try/catch en cada controlador.</summary>
public class ErroresDeNegocioFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ValidacionException v:
                context.Result = new BadRequestObjectResult(new { mensaje = v.Message, errores = v.Errores });
                break;
            case ConflictoException c:
                context.Result = new ConflictObjectResult(new { mensaje = c.Message, detalles = c.Detalles });
                break;
            case NoEncontradoException n:
                context.Result = new NotFoundObjectResult(new { mensaje = n.Message });
                break;
            default:
                return;
        }

        context.ExceptionHandled = true;
    }
}
