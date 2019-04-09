import logging

import coreapi
from rest_framework import fields, serializers
from rest_framework.parsers import BaseParser
from rest_framework.response import Response
from rest_framework.schemas import ManualSchema
from rest_framework.views import APIView
import coreschema

from .adapter import build_response, get_image

log = logging.getLogger(__name__)


class FaceApiOptions(serializers.Serializer):
    Attributes = fields.BooleanField(default=True)
    Descriptor = fields.BooleanField(default=True)
    Emotions = fields.BooleanField(default=True)
    Headpose = fields.BooleanField(default=True)


class BinaryParser(BaseParser):
    media_type = 'application/octet-stream'

    def parse(self, stream, media_type=None, parser_context=None):
        return stream.read()


class FaceApi(APIView):
    """

    **Описание**

        Получить информацию обо всех лицах на изображении.

    **Пример запроса**

        POST /faces/?Descriptor=false

    **Параметры запроса**

        * Attributes: query, получить gender и age лица

        * Emotions: query, получить оценку эмоций лица

        * Headpose: query, получить оценку поворота головы

        * Descriptor: query, получить 256-float вектор, идентифицирующий человека

        * Body: изображение, кодированное base64

    **Параметры ответа**

        Список лиц, для каждого ключи с объектами

        * Attributes: { "Age": <int>, "Gender": "Male"/"Female" }

        * Emotions: {'Anger': <float>, 'Happiness':<float>, 'Neutral':<float>, 'Sadness':<float>, 'Surprise':<float>,
        'Contempt':<float>, 'Disgust':<float>, 'Fear':<float>}

        * Headpose: {"Yaw":<float>, "Roll:<float>, "Pitch":<float>}

        * Descriptor: [<float>][256]
    """
    schema = ManualSchema(
        fields=[
            coreapi.Field(
                name='Attributes',
                location='query',
                description='Flag if return age and gender attributes',
                type=coreschema.Boolean()
            ),
            coreapi.Field(
                name='Emotions',
                location='query',
                description='Flag if return emotions',
                type=coreschema.Boolean()
            ),
            coreapi.Field(
                name='Descriptor',
                location='query',
                description='Flag if return face descriptor',
                type=coreschema.Boolean()
            ),
            coreapi.Field(
                name='Headpose',
                location='query',
                description='Flag if return face head pose',
                type=coreschema.Boolean()
            ),
            coreapi.Field(
                name='Image',
                location="body",
                description='Base64 encoded image',
            )
        ],
        encoding="application/octet-stream"
    )

    _CANT_DECODE_ERROR_MESSAGE = "Failed to decode image"
    parser_classes = (BinaryParser,)

    def post(self, request):
        img = get_image(request)
        if img is None or not len(img):
            return Response(
                {"error": self._CANT_DECODE_ERROR_MESSAGE},
                status=400
            )
        options = FaceApiOptions(data=request.query_params)
        if not options.is_valid():
            return Response(
                data=options.errors,
                status=400
            )
        return Response(build_response(img, options.validated_data), status=200)
