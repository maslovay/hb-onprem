
using System.Threading;
using Configurations;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Base;
using Serilog;using System;
using Swashbuckle.AspNetCore.Swagger;
using UnitTestExtensions;
using Notifications.Services;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace LogSave
{
        public class FormFileSwaggerFilter : IOperationFilter
    {
        private const string formDataMimeType = "multipart/form-data";
        private static readonly string[] formFilePropertyNames =
            typeof(IFormFile).GetTypeInfo().DeclaredProperties.Select(p => p.Name).ToArray();
    
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var parameters = operation.Parameters;
            if (parameters == null || parameters.Count == 0) return;
    
            var formFileParameterNames = new List<string>();
            var formFileSubParameterNames = new List<string>();
    
            foreach (var actionParameter in context.ApiDescription.ActionDescriptor.Parameters)
            {
                var properties =
                    actionParameter.ParameterType.GetProperties()
                        .Where(p => p.PropertyType == typeof(IFormFile))
                        .Select(p => p.Name)
                        .ToArray();
    
                if (properties.Length != 0)
                {
                    formFileParameterNames.AddRange(properties);
                    formFileSubParameterNames.AddRange(properties);
                    continue;
                }
    
                if (actionParameter.ParameterType != typeof(IFormFile)) continue;
                formFileParameterNames.Add(actionParameter.Name);
            }
    
            if (!formFileParameterNames.Any()) return;
    
            var consumes = operation.Consumes;
            consumes.Clear();
            consumes.Add(formDataMimeType);
    
            foreach (var parameter in parameters.ToArray())
            {
                if (!(parameter is NonBodyParameter) || parameter.In != "formData") continue;
    
                if (formFileSubParameterNames.Any(p => parameter.Name.StartsWith(p + "."))
                    || formFilePropertyNames.Contains(parameter.Name))
                    parameters.Remove(parameter);
            }
    
            foreach (var formFileParameter in formFileParameterNames)
            {
                parameters.Add(new NonBodyParameter()
                {
                    Name = formFileParameter,
                    Type = "file",
                    In = "formData"
                });
            }
        }
    }
}