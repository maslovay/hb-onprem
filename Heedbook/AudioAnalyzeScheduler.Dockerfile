FROM microsoft/dotnet:2.2-sdk-alpine AS build-env
WORKDIR /app

COPY . .


RUN dotnet restore ./HBOperations/AudioAnalyzeScheduler/
RUN dotnet build ./HBOperations/AudioAnalyzeScheduler/

# Copy everything else and build
RUN dotnet publish ./HBOperations/AudioAnalyzeScheduler -c Release -o publish
#RUN cp ./HBOperations/AudioAnalyzeScheduler/sentimental/* ./HBOperations/AudioAnalyzeScheduler/bin/Release/netcoreapp2.2 -R

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime-alpine
WORKDIR /app
COPY --from=build-env /app/HBOperations/AudioAnalyzeScheduler/publish .
RUN mkdir -p /app/sentimental/
COPY --from=build-env /app/HBOperations/AudioAnalyzeScheduler/sentimental/ /app/sentimental/

RUN apk add --update python3
RUN apk add --update git
RUN pip3 install --upgrade pip
RUN apk add --update python3-dev

RUN apk add build-base
RUN apk add --update gcc
RUN pip install git+https://github.com/devgopher/sentimental_w_stemmer.git
RUN pip install nltk
 
ENTRYPOINT ["dotnet", "AudioAnalyzeScheduler.dll"]
EXPOSE 53525
ENV ASPNETCORE_URLS http://+:53525
RUN mkdir -p /opt/
RUN chmod -R 777 /opt/
RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
ENV INFRASTRUCTURE OnPrem
