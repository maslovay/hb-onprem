FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .

RUN apt-get update
RUN apk add libgdiplus-dev --update-cache --repository http://dl-3.alpinelinux.org/alpine/edge/testing/ --allow-untrusted  
RUN apk add --no-cache icu-dev libc6-compat icu-libs libgdiplus  

RUN dotnet publish ./HBOperations/ExtractFramesFromVideoService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/ExtractFramesFromVideoService/publish .
ENTRYPOINT ["dotnet", "ExtractFramesFromVideoService.dll"]
RUN apk add ffmpeg
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
ENV TESTCLUSTER testcluster
