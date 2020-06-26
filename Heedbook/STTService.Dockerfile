FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/STTService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/STTService/publish .
COPY --from=build-env /app/HBOperations/STTService/stt.py /app/stt.py

RUN apk add --update python3
RUN pip3 install --upgrade pip
RUN pip3 install asyncio websockets

ENTRYPOINT ["dotnet", "STTService.dll"]
EXPOSE 53653
ENV ASPNETCORE_URLS http://+:53653
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download