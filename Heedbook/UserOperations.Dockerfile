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
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download

ENV TESTCLUSTER testcluster
ENV ELASTIC_SETTINGS_FUNCTION_NAME HbApi
ENV SMTP_SETTINGS_HOST smtp.yandex.ru
ENV SMTP_SETTINGS_PORT 587
ENV SMTP_SETTINGS_FROM_EMAIL support@heedbook.com
ENV SMTP_SETTINGS_PASSWORD Heedbook_2017
ENV SMTP_SETTINGS_DELIVERY_METHOD 0
ENV SMTP_SETTINGS_ENABLE_SSL true
ENV SMTP_SETTINGS_USE_DEFAULT_CREDENTIALS false
ENV SMTP_SETTINGS_TIMEOUT 0
