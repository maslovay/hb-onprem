FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
RUN dotnet publish ./HBOperations/ExtractFramesFromVideoService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/ExtractFramesFromVideoService/publish .
ENTRYPOINT ["dotnet", "ExtractFramesFromVideoService.dll"]
EXPOSE 53575
ENV ASPNETCORE_URLS http://+:53575
RUN apk add ffmpeg
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
RUN chmod g+rwx /app
ENV TESTCLUSTER testcluster
