FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/AudioAnalyzeService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/AudioAnalyzeService/publish .
ENTRYPOINT ["dotnet", "AudioAnalyzeService.dll"]
EXPOSE 53500
ENV ASPNETCORE_URLS http://+:53500
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
ENV INFRASTRUCTURE OnPrem
ENV TESTCLUSTER testcluster
