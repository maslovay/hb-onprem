import json
import numpy as np


class DetectorResult:
    FACE_DETECTOR_NAME = "face_detector"
    HEADPOSE_ESTIMATOR_NAME = "headpose_estimator"
    LANDMARK_DETECTOR_NAME = "landmark_estimator"
    FACE_IDENTIFIER_NAME = "face_identifier"
    AGE_GENDER_ESTIMATOR_NAME = "agegender_estimator"
    EMOTION_ESTIMATOR_NAME = "emotion_estimator"

    _ALL_KEYS = (
        FACE_DETECTOR_NAME, HEADPOSE_ESTIMATOR_NAME, LANDMARK_DETECTOR_NAME,
        FACE_IDENTIFIER_NAME, AGE_GENDER_ESTIMATOR_NAME, EMOTION_ESTIMATOR_NAME
    )

    def to_repr(self):
        raise NotImplementedError()

    def __repr__(self):
        return json.dumps(self.to_repr())


class FaceDetectorResult(DetectorResult):
    OUTPUT_SIZE = 7
    KEYS = ('x', 'y', 'w', 'h', 'conf')

    def __init__(self, output):
        self.image_id = output[0]
        self.label = int(output[1])
        self.confidence = output[2]
        self.position = np.array((output[3], output[4]))  # (x, y)
        self.size = np.array((output[5], output[6]))  # (w, h)

    def __repr__(self):
        return "FaceDetectorResult<id({}),label({}),conf({}),pos({}),size({})>".format(
            self.image_id, self.label, self.confidence, self.position, self.size
        )

    def to_repr(self):
        """
        x, y, w, h, conf
        """
        return {
            self.KEYS[0]: float(self.position[0]),
            self.KEYS[1]: float(self.position[1]),
            self.KEYS[2]: float(self.size[0]),
            self.KEYS[3]: float(self.size[1]),
            self.KEYS[4]: float(self.confidence)
        }


class LandmarkDetectorResult(DetectorResult):
    KEYS = ("left_eye", "right_eye", "nose_tip", "left_lip_corner", "right_lip_corner")

    def __init__(self, outputs):
        self.points = outputs
        p = lambda i: self[i]
        self.left_eye = p(0)
        self.right_eye = p(1)
        self.nose_tip = p(2)
        self.left_lip_corner = p(3)
        self.right_lip_corner = p(4)

    def __getitem__(self, idx):
        return self.points[idx]

    def to_repr(self):
        return dict(
            (k, getattr(self, k).tolist()) for k in self.KEYS
        )


class HeadPoseDetectorResult(DetectorResult):
    KEYS = ("pitch", "yaw", "roll")

    def __init__(self, pitch, yaw, roll):
        self.pitch = pitch
        self.yaw = yaw
        self.roll = roll

    def to_repr(self):
        return dict(
            (k, float(getattr(self, k))) for k in self.KEYS
        )


class FaceIdentifierResult(DetectorResult):
    def __init__(self, id, distance, descriptor=None):
        self.id = id
        self.distance = distance
        self.descriptor = descriptor

    def to_repr(self):
        return {"descriptor": self.descriptor.tolist()}


class AgeGenderResult(DetectorResult):
    KEYS = ("age", "gender")
    THRESHOLD = 0.5

    def __init__(self, age, gender):
        self.age = age
        self.gender = gender

    def to_repr(self):
        return {
            'age': self.age,
            'is_male': self.gender > self.THRESHOLD
        }


class EmotionEstimatorResult(DetectorResult):
    KEYS = ["neutral", "happy", "sad", "surprise", "anger"]

    def __init__(self, emotions):
        if emotions is None:
            emotions = [0.2, 0.2, 0.2, 0.2, 0.2]
        self.emotions = emotions

    def to_repr(self):
        return dict((key, self.emotions[num]) for num, key in enumerate(self.KEYS))


class Detection(DetectorResult):

    def __init__(self, **kwargs):
        for k in self._ALL_KEYS:
            val = kwargs.pop(k, [])
            setattr(self, k, val)
        if len(kwargs):
            raise ValueError("Unknown keys in Detection kwargs {}".format(list(kwargs.keys())))

    @classmethod
    def build_from_results(cls, results_dict, excluded_fields=tuple()):
        if not len(results_dict.get(cls.FACE_DETECTOR_NAME, [])):
            return []
        detection_length = len(results_dict[cls.FACE_DETECTOR_NAME])
        for k, v in results_dict.items():
            if not len(v) == detection_length:
                raise ValueError("Different length for different detections types")

        detections = []
        # Always need cls.FACE_DETECTOR_NAME, never need cls.LANDMARK_DETECTOR_NAME
        fields_to_build = {x for x in cls._ALL_KEYS} - {x for x in excluded_fields} \
                          | {cls.FACE_DETECTOR_NAME}
        # Use only available fields
        fields_to_build = fields_to_build & set(results_dict.keys())
        for idx in range(detection_length):
            kwargs = dict(
                (k, results_dict[k][idx].to_repr()) for k in fields_to_build
            )
            detections.append(cls(**kwargs))
        return detections

    def __getitem__(self, key):
        if not key in self._ALL_KEYS:
            raise KeyError("Key must be in {}".format(self._ALL_KEYS))
        return getattr(self, key)

    def to_repr(self):
        total = {}
        for k in self._ALL_KEYS:
            v = getattr(self, k, None)
            if v:
                total[k] = v
        return total
