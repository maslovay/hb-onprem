FROM python:3.6

WORKDIR /app

COPY requirements.txt /app/requirements.txt
RUN pip install -r requirements.txt

COPY . /app
EXPOSE 8000
CMD ["python","-u", "/app/manage.py", "collectstatic"]
CMD ["python","-u", "/app/manage.py", "runserver", "0.0.0.0:8000"]

