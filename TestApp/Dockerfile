FROM microsoft/dotnet:2.1-sdk

RUN mkdir -p /service/lib /service/app /filedump

ADD hblibonprem/hb-onprem/HBLibOnprem/HBLibOnprem /service/lib

ADD Local /service/app

RUN dotnet restore -f /service/lib/HBLibOnprem.csproj

RUN dotnet add /service/app/Local.csproj reference /service/lib/HBLibOnprem.csproj

RUN dotnet restore -f /service/app/Local.csproj

RUN dotnet build --force -f netcoreapp2 -c Release /service/app/Local.csproj

WORKDIR /filedump

CMD ["dotnet", "run", "--no-build", "-c", "Release", "-p", "/service/app/Local.csproj"]
