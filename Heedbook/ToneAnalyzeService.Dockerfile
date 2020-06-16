FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/ToneAnalyzeService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/ToneAnalyzeService/publish .
ENTRYPOINT ["dotnet", "ToneAnalyzeService.dll"]
EXPOSE 53535
ENV ASPNETCORE_URLS http://+:53535
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
RUN apk update
RUN apk add wine xvfb
RUN apk add ffmpeg
RUN chmod +x /app/OpenVokaWavMean-3-0-win64.exe
ENV TESTCLUSTER testcluster
