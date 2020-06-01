FROM python:3.6

RUN pip install Flask==1.1.2 websockets flask-restplus==0.13.0 asyncio Werkzeug==0.16.1
WORKDIR /app
COPY app /app
EXPOSE 2700 8118

RUN mkdir -p /opt/download
RUN chmod -R 777 /opt/download
CMD ["python", "asr.py"]
