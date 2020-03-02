FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBSenders/OnlineTuiOfficesScheduler -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBSenders/OnlineTuiOfficesScheduler/publish .
ENTRYPOINT ["dotnet", "OnlineTuiOfficesScheduler.dll"]
ENV TESTCLUSTER testcluster
