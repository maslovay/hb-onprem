FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app

COPY . .

RUN dotnet restore ./HBOperations/DialoguesRecalculateScheduler/
RUN dotnet build ./HBOperations/DialoguesRecalculateScheduler/

# Copy everything else and build
RUN dotnet publish ./HBOperations/DialoguesRecalculateScheduler -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/DialoguesRecalculateScheduler/publish .
                                                                         
ENTRYPOINT ["dotnet", "DialoguesRecalculateScheduler.dll"]