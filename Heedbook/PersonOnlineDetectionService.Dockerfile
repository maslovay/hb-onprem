FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/PersonOnlineDetectionService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/PersonOnlineDetectionService/publish .
COPY --from=build-env /app/HBOperations/PersonOnlineDetectionService/websocketio.py /app/websocketio.py

RUN apk add --update python3
RUN pip3 install --upgrade pip
RUN pip3 install "python-socketio[client]" 

ENTRYPOINT ["dotnet", "PersonOnlineDetectionService.dll"]
EXPOSE 52823
ENV ASPNETCORE_URLS http://+:52823

RUN apk add libgdiplus --update-cache --repository http://dl-3.alpinelinux.org/alpine/edge/testing/ --allow-untrusted
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
ENV TESTCLUSTER testcluster
