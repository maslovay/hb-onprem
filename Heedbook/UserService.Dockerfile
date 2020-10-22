FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/UserService -c Release -o publish
# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
RUN mkdir InitializeDBTables
COPY --from=build-env /app/HBOperations/UserService/Phrases.xlsx Phrases.xlsx
COPY --from=build-env /app/HBOperations/UserService/publish .
COPY --from=build-env /app/HBOperations/UserService/InitializeDBTables InitializeDBTables
#COPY --from=build-env /app/HBOperations/UserService/Phrases.xlsx Phrases.xlsx
ENTRYPOINT ["dotnet", "UserService.dll"]
EXPOSE 53650
ENV ASPNETCORE_URLS http://+:53650
RUN apk add libgdiplus --update-cache --repository http://dl-3.alpinelinux.org/alpine/edge/testing/ --allow-untrusted
RUN apk add ffmpeg
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
ENV TESTCLUSTER testcluster
