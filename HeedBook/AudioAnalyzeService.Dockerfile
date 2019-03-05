FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./AudioAnalyzeService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/AudioAnalyzeService/publish .
ENTRYPOINT ["dotnet", "AudioAnalyzeService.dll"]
RUN mkdir /opt/
RUN chmod -R 777 /opt/
RUN mkdir /opt/download
RUN chmod -R 777 /opt/download
RUN apk update
RUN apk add wine xvfb
RUN apk add ffmpeg
RUN chmod +x /app/OpenVokaWavMean-3-0-win64.exe

