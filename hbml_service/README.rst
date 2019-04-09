Описание
--------
Heedbook Onprem FaceApi: Django-сервис вокруг OpenVino моделей для анализа лиц на изображении.
Запускается через docker-compose в паре с nginx-контейнером. В deploy/check_service_example.ipynb находится пример
использования сервиса.

Опции
-----

В образ могут быть проброшены несколько переменных окружения:

 - DEBUG - режим работы сервиса
 - ALLOWED_HOSTS - разрешенные host для запросов
 - OPENVINO_CONFIG - json пар ключ-значение для OpenVino

Запуск через docker
-------------------
1. Скачать контейнер

  ::

    docker pull heedbookregistry.azurecr.io/heedbookdev/hbml_service:v2

2. Запустить контейнер. Чтобы без nginx показывалась статика в документации, нужно передать DEBUG=true


  ::

    docker run -e DEBUG='true' -p 8000:8000 heedbookregistry.azurecr.io/heedbookdev/hbml_service:v2


Запуск через docker-compose
---------------------------

1.Скачать репозиторий

  ::

    git clone https://github.com/maslovay/hbml_service.git


2а.(Если собирать контейнер) Запустить подготовительный скрипт, который скачает OpenVino(~1.5Gb)

   ::

    ./deploy/prepare_build.sh

2б.(Если не собирать контейнер) Скачать контейнер

  ::

    docker pull heedbookregistry.azurecr.io/heedbookdev/hbml_service:v2



3.Запустить. В варианте А при первом запуске контейнер будет собираться минут 5-10

  ::

    docker-compose up


