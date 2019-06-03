FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .

RUN dotnet restore ./HBOperations/DialogueStatusCheckerScheduler
RUN dotnet build  ./HBOperations/DialogueStatusCheckerScheduler

# Copy everything else and build
RUN dotnet publish ./HBOperations/DialogueStatusCheckerScheduler -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/DialogueStatusCheckerScheduler/publish .
ENTRYPOINT ["dotnet", "DialogueStatusCheckerScheduler.dll"]
