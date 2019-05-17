FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
RUN dotnet publish ./HBAPI/ReferenceController -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBAPI/ReferenceController/publish .
ENTRYPOINT ["dotnet", "ReferenceController.dll"]
EXPOSE 53655
ENV ASPNETCORE_URLS http://+:53655
RUN mkdir /opt/
RUN chmod -R 777 /opt/
RUN mkdir /opt/download
RUN chmod -R 777 /opt/download
