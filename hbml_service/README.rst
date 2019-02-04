Запуск
------

0. Утащить репозиторий

  ::

    git clone https://github.com/maslovay/hbml_service.git

1. Утащить docker image

  ::

    docker pull heedbookregistry.azurecr.io/heedbookdev/hbml_service:latest


3. Запустить контейнер

  ::

    docker run -e DEBUG='true' -p 8000:8000 heedbookregistry.azurecr.io/heedbookdev/hbml_service

  Сервис принимает запросы по 0.0.0.0 и по localhost. Если нужен другой hostname, например 127.0.0.1, нужно передать его в переменной -e ALLOWED_HOSTS='["127.0.0.1"]'

4. (Опционально) Прогнать тесты внутри контейнера

  ::

    docker container ls
    docker exec -it %CONTAINER_ID_контейнера_hbml_service% bash
    python manage.py test

5. Зайти на 0.0.0.0:8000, посмотреть документацию API (статика подтягивается только при DEBUG=true)
