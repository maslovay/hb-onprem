FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/UserService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/UserService/publish .
ENTRYPOINT ["dotnet", "UserService.dll"]
EXPOSE 53650
ENV ASPNETCORE_URLS http://+:53650
RUN mkdir /opt/
RUN chmod -R 777 /opt/
RUN mkdir /opt/download
RUN chmod -R 777 /opt/download
