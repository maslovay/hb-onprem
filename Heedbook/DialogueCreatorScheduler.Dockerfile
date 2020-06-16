FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/DialogueCreatorScheduler -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/DialogueCreatorScheduler/publish .
ENTRYPOINT ["dotnet", "DialogueCreatorScheduler.dll"]
EXPOSE 54822
ENV ASPNETCORE_URLS http://+:54822
ENV TESTCLUSTER testcluster
