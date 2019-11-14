FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBAPI/HeedbookAPI -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBAPI/HeedbookAPI/publish .
ENTRYPOINT ["dotnet", "UserOperations.dll"]
EXPOSE 53651
ENV ASPNETCORE_URLS http://+:53651
RUN chmod -R 777 /opt/
RUN mkdir /opt/download
RUN chmod -R 777 /opt/download
ENV TESTCLUSTER testcluster
