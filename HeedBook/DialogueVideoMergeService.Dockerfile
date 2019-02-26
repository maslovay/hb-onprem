FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app
COPY . .
RUN dotnet publish ./DialogueVideoMergeService -c Release -o publish

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/DialogueVideoMerge/publish .
ENTRYPOINT ["dotnet", "DialogueVideoMergeService.dll"]
RUN apk add ffmpeg