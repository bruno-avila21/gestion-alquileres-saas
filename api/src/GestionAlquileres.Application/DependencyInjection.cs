using System.Reflection;
using FluentValidation;
using GestionAlquileres.Application.Common.Behaviors;
using GestionAlquileres.Application.Common.Billing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GestionAlquileres.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(cfg => { }, assembly);

        // Scoped: comparte el DbContext del request (o del scope del job), para que el
        // devengamiento y la imputación del pago caigan en la misma unidad de trabajo.
        services.AddScoped<ILateFeeAccrualService, LateFeeAccrualService>();

        return services;
    }
}
