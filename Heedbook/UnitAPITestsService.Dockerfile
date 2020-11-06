FROM microsoft/dotnet:2.2-sdk-alpine AS build-env

WORKDIR /app
COPY . .
RUN dotnet publish ./HBOperations/ApiTests -c Release -o publish
RUN dotnet publish ./HBOperations/UnitAPITestsService -c Release -o publish

FROM microsoft/dotnet:2.2-sdk-alpine

WORKDIR /app

COPY --from=build-env /app/HBOperations/ApiTests/publish .
COPY --from=build-env /app/HBOperations/UnitAPITestsService/publish .
#WORKDIR /app/HBOperations/UnitAPITestsService
ENTRYPOINT ["dotnet", "UnitAPITestsService.dll"]

RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
ENV VSTEST_CONNECTION_TIMEOUT 90000
ENV DOCKER_UNIT_TEST_ENVIRONMENT TRUE
ENV DOCKER_INTEGRATION_TEST_ENVIRONMENT TRUE
ENV DB_CONNECTION_STRING "User ID=test_user;Password=test_password;Host=104.40.181.96;Port=5432;Database=test_db;Pooling=true;Keepalive=25"
ENV RABBITMQ_CONNECTION_USERNAME "guest"
ENV RABBITMQ_CONNECTION_PASSWORD "guest"
ENV RABBITMQ_CONNECTION_PORT "5672"
ENV RABBITMQ_CONNECTION_VHOST "test"
ENV RABBITMQ_CONNECTION_HOSTNAME "localhost"
ENV SFTP_CONNECTION_HOST "hbftptest.westeurope.cloudapp.azure.com"
ENV SFTP_CONNECTION_PORT "22"
ENV SFTP_CONNECTION_USERNAME "nkrokhmal"
ENV SFTP_CONNECTION_PASSWORD "kloppolk_2018"
ENV SFTP_CONNECTION_DESTINATIONPATH "/home/nkrokhmal/storage/"
ENV SFTP_CONNECTION_DOWNLOADPATH "/opt/download/"
ENV SMTP_SETTINGS_FROM_EMAIL "support@heedbook.com"
ENV SMTP_SETTINGS_PASSWORD "Heedbook_2017"
ENV SMTP_SETTINGS_HOST "smtp.yandex.ru"
ENV SMTP_SETTINGS_PORT "587"
ENV SMTP_SETTINGS_DELIVERY_METHOD "0"
ENV SMTP_SETTINGS_ENABLE_SSL "true"
ENV SMTP_SETTINGS_USE_DEFAULT_CREDENTIALS "false"
ENV SMTP_SETTINGS_TIMEOUT "10000"
ENV URL_SETTINGS_HOST "https://heedbookapitest.westeurope.cloudapp.azure.com/"
ENV ELASTIC_SETTINGS_HOST "tcp://logstash-service.kube-system"
ENV ELASTIC_SETTINGS_PORT "5000"

