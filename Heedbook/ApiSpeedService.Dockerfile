FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
COPY HBOperations/ApiSpeed/TestImage.jpg /app/
# Copy everything else and build
RUN dotnet publish ./HBOperations/ApiSpeed -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/ApiSpeed/publish .
ENTRYPOINT ["dotnet", "ApiSpeed.dll"]
EXPOSE 53900
ENV ASPNETCORE_URLS http://+:53900
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
ENV TESTCLUSTER testcluster
