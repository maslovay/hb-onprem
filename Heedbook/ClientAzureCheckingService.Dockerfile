FROM ubuntu:18.04
FROM microsoft/dotnet:2.2-sdk AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/ClientAzureCheckingService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/HBOperations/ClientAzureCheckingService/publish .
ENTRYPOINT ["dotnet", "ClientAzureCheckingService.dll"]
RUN chmod -R 777 /opt/
RUN mkdir /opt/download
RUN chmod -R 777 /opt/download
RUN apt-get update
ENV TESTCLUSTER testcluster