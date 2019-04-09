import logging
import base64
import numpy as np
import cv2
from django.conf import settings
from hbml_service.ov_faceapi import Detection, FaceApiModule, build_config


log = logging.getLogger(__name__)
faceapi_module = FaceApiModule(build_config(**settings.OPENVINO_FACEAPI_CONFIG))


class FaceApiReprAdaptor:
    ADAPTATION_MAP = {
        Detection.FACE_DETECTOR_NAME: "Rectangle",
        Detection.AGE_GENDER_ESTIMATOR_NAME: "Attributes",
        Detection.HEADPOSE_ESTIMATOR_NAME: "Headpose",
        Detection.EMOTION_ESTIMATOR_NAME: "Emotions",
        Detection.LANDMARK_DETECTOR_NAME: "Landmarks"
    }

    def __init__(self, detection_repr, excluded_fields=(Detection.LANDMARK_DETECTOR_NAME,)):
        self.detection_repr = detection_repr
        self.excluded_fields = excluded_fields

    def adapted(self):
        repr_ = {}
        repr_.update(self.get_rectangle_repr())
        repr_.update(self.get_emotions_repr())
        repr_.update(self.get_attributes_repr())
        repr_.update(self.get_headpose_repr())
        repr_.update(self.get_descriptor_repr())
        return repr_

    def get_rectangle_repr(self):
        if Detection.FACE_DETECTOR_NAME in self.excluded_fields:
            return {}
        data = self.detection_repr[Detection.FACE_DETECTOR_NAME]
        return {"Rectangle": {
            "Top": int(data['y']),
            "Left": int(data['x']),
            "Width": int(data['w']),
            "Height": int(data['h'])
        }}

    def get_attributes_repr(self):
        if Detection.AGE_GENDER_ESTIMATOR_NAME in self.excluded_fields:
            return {}
        data = self.detection_repr[Detection.AGE_GENDER_ESTIMATOR_NAME]
        return {"Attributes": {
            "Age": int(data['age']),
            "Gender": "Male" if data['is_male'] else 'Female'
        }}

    def get_headpose_repr(self):
        if Detection.HEADPOSE_ESTIMATOR_NAME in self.excluded_fields:
            return {}
        data = self.detection_repr[Detection.HEADPOSE_ESTIMATOR_NAME]
        return {"Headpose":{
            "Pitch": round(data['pitch'], 1),
            "Roll": round(data['roll'], 1),
            "Yaw": round(data['yaw'], 1),
        }}

    def get_emotions_repr(self):
        if Detection.EMOTION_ESTIMATOR_NAME in self.excluded_fields:
            return {}
        data = self.detection_repr[Detection.EMOTION_ESTIMATOR_NAME]
        return {"Emotions":{
            "Anger": round(data["anger"], 3),
            "Happiness": round(data["happy"], 3),
            "Neutral": round(data["neutral"], 3),
            "Sadness": round(data["sad"], 3),
            "Surprise": round(data["surprise"], 3),
            "Contempt": 0.0,
            "Disgust": 0.0,
            "Fear": 0.0
        }}

    def get_descriptor_repr(self):
        if Detection.FACE_IDENTIFIER_NAME in self.excluded_fields:
            return {}
        data = self.detection_repr[Detection.FACE_IDENTIFIER_NAME]
        return {"Descriptor": data["descriptor"]}


def get_image(request):
    """
    Декодирует изображение из request.data.
    """
    try:
        image_data = base64.b64decode(request.data)
        img_buffer = np.frombuffer(image_data, dtype='uint8')
        return cv2.imdecode(img_buffer, cv2.IMREAD_UNCHANGED)
    except Exception as e:
        log.error(str(e))
        return None


def build_response(np_image, options):
    excluded_fields = []
    if not options['Attributes']:
        excluded_fields.append(Detection.AGE_GENDER_ESTIMATOR_NAME)
    if not options['Emotions']:
        excluded_fields.append(Detection.EMOTION_ESTIMATOR_NAME)
    if not options['Headpose']:
        excluded_fields.append(Detection.HEADPOSE_ESTIMATOR_NAME)
    if not options['Descriptor']:
        excluded_fields.append(Detection.LANDMARK_DETECTOR_NAME)
        excluded_fields.append(Detection.FACE_IDENTIFIER_NAME)
    detections = faceapi_module.detect(np_image, excluded_fields)
    return [FaceApiReprAdaptor(d.to_repr(), excluded_fields=excluded_fields).adapted() for d in detections]
