FROM microsoft/dotnet:2.2-sdk AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/UnitAPITestsService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-sdk
WORKDIR /app
COPY --from=build-env /app/HBOperations/UnitAPITestsService/publish .
ENTRYPOINT ["dotnet", "UnitAPITestsService.dll"]
#RUN mkdir -p /ApiTests/bin/Debug/netcoreapp2.2/
#RUN chmod -R 777 /ApiTests/bin/Debug/netcoreapp2.2/
#COPY /ApiTests/bin/Debug/netcoreapp2.2/ApiTests.dll .
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
ENV TESTCLUSTER testcluster
