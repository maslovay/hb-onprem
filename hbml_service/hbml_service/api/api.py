import base64
import logging

from django.conf import settings
from rest_framework.response import Response
from rest_framework.views import APIView

from .parser import BinaryParser

log = logging.getLogger(__name__)


class ImageProcessorApi(APIView):
    _CANT_DECODE_ERROR_MESSAGE = "Failed to decode image"
    parser_classes = (BinaryParser,)

    def get_image(self, request):
        """
        Пытается декодировать изображение из request.data.
        """
        try:
            image = base64.b64decode(request.data)
            return image
        except Exception as e:
            log.error(str(e))
            return None


class FaceEmotionsApi(ImageProcessorApi):
    """
    Compute emotions at faces in image.
    Image is taken from base64 decoded POST body.
    """
    def post(self, request):
        img = self.get_image(request)
        if img is None or not len(img):
            return Response(
                {"error": self._CANT_DECODE_ERROR_MESSAGE},
                status=400
            )
        # TODO: убрать stub
        emotions_keys = settings.FACE_EMOTIONS_KEYS
        stub = dict((k, 0) for k in emotions_keys)
        stub['Neutral'] = 0.7
        stub['Happiness'] = 0.3
        stub = [stub]
        return Response(stub, status=200)


class FaceAttributesApi(ImageProcessorApi):
    """
    Compute face attribute(gender, age) at faces in image.
    Image is taken from base64 decoded POST body.
    """

    def post(self, request):
        img = self.get_image(request)
        if img is None or not len(img):
            return Response(
                {"error": self._CANT_DECODE_ERROR_MESSAGE},
                status=400
            )
        # TODO: убрать stub
        attributes_keys = settings.FACE_ATTRIBUTES_KEYS
        stub = dict((k, 0) for k in attributes_keys)
        stub['Gender'] = 'Male'
        stub['Age'] = 25
        stub = [stub]
        return Response(stub, status=200)
