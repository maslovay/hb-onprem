FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/ToneAnalyzeService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
#RUN addgroup -g 2000 docker \
#    && adduser -u 2000 -G docker -s /bin/sh -D docker

COPY --from=build-env /app/HBOperations/ToneAnalyzeService/publish .
#ENTRYPOINT ["dotnet", "ToneAnalyzeService.dll"]
#EXPOSE 53535
#ENV ASPNETCORE_URLS http://+:53535
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
RUN apk update
RUN apk add wine xvfb
RUN apk add ffmpeg
RUN chmod +x /app/OpenVokaWavMean-3-0-win64.exe
RUN wget https://hbftptest.westeurope.cloudapp.azure.com/dialogueaudios/01d70dc6-ea24-4f16-940a-53308bc1eca3.wav
#USER 1001
RUN export WINEPREFIX=/app/.wine && wine64 winecfg && chown -R 1000310000: /app/.wine
RUN chmod -R 777 /app/.wine && chown -R 1000310000: /app/.wine
RUN WINEPREFIX=/app/.wine
ENV WINEPREFIX /app/.wine
USER 1000310000

#COPY --from=build-env /app/HBOperations/ToneAnalyzeService/publish .
ENTRYPOINT ["dotnet", "ToneAnalyzeService.dll"]
EXPOSE 53535
ENV ASPNETCORE_URLS http://+:53535
ENV TESTCLUSTER testcluster
