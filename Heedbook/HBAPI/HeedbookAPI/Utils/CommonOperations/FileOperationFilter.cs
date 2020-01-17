using HBData.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using UserOperations.Controllers;
using UserOperations.Models;

namespace UserOperations.Utils
{
    //---its specially for swagger---
    public class FileOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ParameterDescriptions.Any(x => x.ModelMetadata?.ModelType == typeof(Microsoft.AspNetCore.Http.IFormCollection)))
            {
                var apiParametrDescription = context.ApiDescription.ParameterDescriptions.FirstOrDefault(x => x.ModelMetadata.ModelType == typeof(Microsoft.AspNetCore.Http.IFormCollection));
                var paramForRemove = operation.Parameters.FirstOrDefault(x => x.Name == apiParametrDescription.Name);
                operation.Parameters.Remove(paramForRemove);
                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = "File",
                    In = "formData",
                    Description = "Upload file.",
                    Required = false,
                    Type = "file"
                });
                if (context.ApiDescription.ActionDescriptor.AttributeRouteInfo.Template == "api/User/User")
                {
                    operation.Parameters.Add(new NonBodyParameter
                    {
                        Name = "data",
                        In = "formData",
                        Type = "object",
                        Default = JsonConvert.SerializeObject(new PostUser() { CompanyId=Guid.NewGuid(), Email="uniqueField", Password="required", RoleId=Guid.NewGuid()}),
                        Description = "fill the user fields",
                        Required = true
                    });
                    operation.Consumes.Add("application/form-data");
                }

                if (context.ApiDescription.ActionDescriptor.AttributeRouteInfo.Template == "api/CampaignContent/Content")
                {
                    operation.Parameters.Add(new NonBodyParameter
                    {
                        Name = "data",
                        In = "formData",
                        Type = "object",
                        Default = JsonConvert.SerializeObject(new Content() { ContentId = Guid.NewGuid(), Duration = 0, IsTemplate = false, JSONData = "{}", Name = "", RawHTML = "" }),
                        Description = "fill the user fields",
                        Required = true
                    });
                    operation.Consumes.Add("application/form-data");
                }
            }
        }
    }
}
