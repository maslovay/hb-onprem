from django.urls import path

from .api import FaceAttributesApi, FaceEmotionsApi

urlpatterns = [
    path('face_attributes/', FaceAttributesApi.as_view(), name="face_attributes"),
    path('face_emotions/', FaceEmotionsApi.as_view(), name="face_emotions")
]
