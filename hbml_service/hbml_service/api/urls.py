from django.urls import path

from .api import FaceApi

urlpatterns = [
    path('face/', FaceApi.as_view(), name="face"),
]
