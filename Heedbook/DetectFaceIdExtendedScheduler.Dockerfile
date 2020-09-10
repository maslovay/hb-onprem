FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
# Copy everything else and build
RUN dotnet publish ./HBOperations/DetectFaceIdExtendedScheduler -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/DetectFaceIdExtendedScheduler/publish .
ENTRYPOINT ["dotnet", "DetectFaceIdExtendedScheduler.dll"]
EXPOSE 54811
ENV ASPNETCORE_URLS http://+:54811
ENV TESTCLUSTER testcluster
