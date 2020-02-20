FROM microsoft/dotnet:2.2-sdk AS build-env

WORKDIR /app
COPY . .
RUN dotnet publish ./HBOperations/ApiTests -c Release -o publish
RUN dotnet publish ./HBOperations/UnitAPITestsService -c Release -o publish

WORKDIR /app/HBOperations/ApiTests/
RUN dotnet restore .; exit 0;
RUN dotnet build

WORKDIR /app/HBOperations/UnitAPITestsService/
RUN dotnet restore .; exit 0;
RUN dotnet build

FROM mcr.microsoft.com/dotnet/core/sdk:2.2

WORKDIR /app
COPY --from=build-env /app .

COPY --from=build-env /app/HBOperations/ApiTests/publish .
COPY --from=build-env /app/HBOperations/UnitAPITestsService/publish .
#WORKDIR /app/HBOperations/UnitAPITestsService
ENTRYPOINT ["dotnet", "UnitAPITestsService.dll"]

RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
ENV VSTEST_CONNECTION_TIMEOUT 90000
ENV DOCKER_UNIT_TEST_ENVIRONMENT TRUE
