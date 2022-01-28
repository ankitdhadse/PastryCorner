using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PastryCorner.WebApi.Extensions
{
    using System.Reflection;
    using Autofac;
    using Autofac.Builder;
    using Autofac.Features.Scanning;
    using Microsoft.AspNetCore.SignalR;

    public static class AutoFacExtensions
    {
        public static IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle>
            RegisterHubs(this ContainerBuilder builder, params Assembly[] assemblies)
        {
            return builder.RegisterAssemblyTypes(assemblies)
                .Where(t => typeof(Hub).IsAssignableFrom(t))
                .ExternallyOwned();
        }
    }
}
