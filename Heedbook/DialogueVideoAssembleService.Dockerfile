FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/DialogueVideoAssembleService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/DialogueVideoAssembleService/publish .
ENTRYPOINT ["dotnet", "DialogueVideoAssembleService.dll"]
RUN apk add ffmpeg
RUN mkdir /opt/
RUN chmod -R 777 /opt/
RUN mkdir /opt/download
RUN chmod -R 777 /opt/download
ENV TESTCLUSTER testcluster
