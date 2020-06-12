FROM mcr.microsoft.com/dotnet/core/sdk:2.2
#FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR app
COPY /Migrator ./Migrator
COPY /HBData ./HBData

RUN dotnet build ./Migrator -c Release -o publish

#FROM mcr.microsoft.com/dotnet/core/sdk:2.2
#WORKDIR /app
#COPY --from=build-env /app/Migrator ./Migrator
#COPY --from=build-env /app/HBData ./HBData

#CMD ["dotnet", "ef", "--startup-project", "./Migrator/Startup.cs", "--project", "./Migrator/Migrator.csproj",  "migrations", "add", "InitialMigration"]
#CMD ["cd", "Migrator"]
CMD ["sh", "-c",  "cd Migrator; dotnet ef database update"]
#CMD ["sleep", "1200"]
