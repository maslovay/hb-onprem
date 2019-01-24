import base64
import os

from django.conf import settings
from django.test import TestCase
from django.urls import reverse


class FaceApiTestCase(TestCase):
    """
    Проверка формата response, возвращаемого API
    """

    _IMG_SIZE_BYTES = 1000
    _CONTENT_TYPE = "application/octet-stream"

    def _get_image_base64(self):
        pseudo_img_content = os.urandom(self._IMG_SIZE_BYTES)
        encoded_img_content = base64.b64encode(pseudo_img_content)
        return encoded_img_content

    def test_face_attributes(self):
        url = reverse("face_attributes")
        response = self.client.post(
            url,
            data=self._get_image_base64(),
            content_type=self._CONTENT_TYPE
        )
        self.assertTrue(response.status_code == 200)
        attributes_keys = settings.FACE_ATTRIBUTES_KEYS
        results = response.json()
        self.assertTrue(isinstance(results, list))
        self.assertTrue(len(results))

        has_right_keys = lambda keys: set(keys) == set(attributes_keys)
        self.assertTrue(all([has_right_keys(r.keys()) for r in results]))

        response = self.client.post(
            url,
            data=None,
            content_type=self._CONTENT_TYPE
        )
        self.assertTrue(response.status_code == 400)

    def test_face_emotions(self):
        url = reverse("face_emotions")
        response = self.client.post(
            url,
            data=self._get_image_base64(),
            content_type=self._CONTENT_TYPE
        )
        self.assertTrue(response.status_code == 200)

        results = response.json()
        self.assertTrue(isinstance(results, list))
        self.assertTrue(len(results))

        emotions_keys = settings.FACE_EMOTIONS_KEYS

        has_right_keys = lambda keys: set(keys) == set(emotions_keys)
        self.assertTrue(all([has_right_keys(r.keys()) for r in results]))

        response = self.client.post(
            url,
            data=None,
            content_type=self._CONTENT_TYPE
        )
        self.assertTrue(response.status_code == 400)
