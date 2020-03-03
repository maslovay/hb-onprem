import logging as log
from time import time

import cv2
import numpy as np
from numpy import clip

from .utils import cut_rois
from .detections import FaceDetectorResult, HeadPoseDetectorResult, \
    AgeGenderResult, LandmarkDetectorResult, FaceIdentifierResult, \
    EmotionEstimatorResult, Detection


class Detector:
    """
    Takes .xml model, deploys it to device and uses for inference
    """
    def __init__(self, model):
        self.model = model
        self.device_model = None

        self.max_requests = 0
        self.active_requests = 0
        self.name = 'undefined'
        self.execution_time_start = 0
        self.clear()

    def deploy(self, device, context, queue_size=1):
        self.context = context
        self.max_requests = queue_size
        self.device_model = context.deploy_model(
            self.model, device, self.max_requests)
        self.model = None

    def enqueue(self, input):
        self.execution_time_start = time()
        self.clear()

        if self.max_requests <= self.active_requests:
            log.warning("Processing request rejected - too many requests")
            return False
        self.device_model.start_async(self.active_requests, {self.input_blob: input})
        self.active_requests += 1
        return True

    def wait(self):
        if self.active_requests <= 0:
            return

        self.perf_stats = [None, ] * self.active_requests
        self.outputs = [None, ] * self.active_requests
        for i in range(self.active_requests):
            self.device_model.requests[i].wait()
            self.outputs[i] = self.device_model.requests[i].outputs
            self.perf_stats[i] = self.device_model.requests[i].get_perf_counts()

        self.active_requests = 0
        log.info("Inference {} ended, took {:1.4f}s".format(self.name, time() - self.execution_time_start))

    def get_outputs(self):
        self.wait()
        return self.outputs

    def get_performance_stats(self):
        return self.perf_stats

    def clear(self):
        self.perf_stats = []
        self.outputs = []

    def _resize(self, frame, target_shape):
        assert len(frame.shape) == len(target_shape), \
            "Expected a frame with %s dimensions, but got %s" % \
            (len(target_shape), len(frame.shape))

        assert frame.shape[0] == 1, "Only batch size 1 is supported"
        n, c, h, w = target_shape

        input = frame[0]
        if not np.array_equal(target_shape[-2:], frame.shape[-2:]):
            input = input.transpose((1, 2, 0))  # to HWC
            input = cv2.resize(input, (w, h))
            input = input.transpose((2, 0, 1))  # to CHW

        return input.reshape((n, c, h, w))


class FaceDetector(Detector):
    """
    Finds faces RoI at image.
    """
    RETAIL_INPUT_SHAPE = [1, 3, 300, 300]
    ADAS_INPUT_SHAPE = [1, 3, 384, 672]

    def __init__(self, model, confidence_threshold=0.5, roi_scale_factor=1.15):
        super(FaceDetector, self).__init__(model)

        assert len(model.inputs) == 1, "Expected 1 input blob"
        assert len(model.outputs) == 1, "Expected 1 output blob"
        self.input_blob = next(iter(model.inputs))
        self.output_blob = next(iter(model.outputs))
        self.input_shape = model.inputs[self.input_blob].shape
        self.output_shape = model.outputs[self.output_blob].shape
        self.name = Detection.FACE_DETECTOR_NAME

        assert np.array_equal(self.ADAS_INPUT_SHAPE, self.input_shape) or \
               np.array_equal(self.RETAIL_INPUT_SHAPE, self.input_shape), \
            "Expected model input shape %s, but got %s" % \
            (" or ".join([self.ADAS_INPUT_SHAPE, self.RETAIL_INPUT_SHAPE]),
             self.input_shape)

        assert len(self.output_shape) == 4 and \
               self.output_shape[3] == FaceDetectorResult.OUTPUT_SIZE, \
            "Expected model output shape with %s outputs" % \
            (FaceDetectorResult.OUTPUT_SIZE)

        assert 0.0 <= confidence_threshold and confidence_threshold <= 1.0, \
            "Confidence threshold is expected to be in range [0; 1]"
        self.confidence_threshold = confidence_threshold

        assert 0.0 < roi_scale_factor, "Expected positive ROI scale factor"
        self.roi_scale_factor = roi_scale_factor

    def get_input_shape(self):
        return self.input_shape

    def preprocess(self, frame):
        assert len(frame.shape) == 4, "Frame shape should be [1, c, h, w]"
        input = self._resize(frame, self.input_shape)
        return input

    def start_async(self, frame):
        input = self.preprocess(frame)
        self.enqueue(input)

    def get_roi_proposals(self, frame):
        outputs = self.get_outputs()[0][self.output_blob]
        # outputs shape is [N_requests, 1, 1, N_max_faces, 7]

        frame_width = frame.shape[-1]
        frame_height = frame.shape[-2]

        results = []
        for output in outputs[0][0]:
            result = FaceDetectorResult(output)
            if result.confidence < self.confidence_threshold:
                break  # results are sorted by confidence decrease

            self._clip(result, 1, 1)
            self._resize_roi(result, frame_width, frame_height)
            self._rescale_roi(result, self.roi_scale_factor)
            self._clip(result, frame_width, frame_height)

            results.append(result)

        return results

    def _rescale_roi(self, result, roi_scale_factor=1.0):
        result.position -= result.size * 0.5 * (roi_scale_factor - 1.0)
        result.size *= roi_scale_factor
        return result

    def _resize_roi(self, result, frame_width, frame_height):
        result.position[0] *= frame_width
        result.position[1] *= frame_height
        result.size[0] = result.size[0] * frame_width - result.position[0]
        result.size[1] = result.size[1] * frame_height - result.position[1]
        return result

    def _clip(self, result, width, height):
        min = [0, 0]
        max = [width, height]
        result.position[:] = clip(result.position, min, max)
        result.size[:] = clip(result.size, min, max)
        return result


class RoiMixin:
    """
    Implements methods that are same for several detectors
    """
    def start_async(self, frame, rois):
        inputs = self.preprocess(frame, rois)
        for input in inputs:
            self.enqueue(input)

    def preprocess(self, frame, rois):
        assert len(frame.shape) == 4, "Frame shape should be [1, c, h, w]"
        inputs = cut_rois(frame, rois)
        inputs = [self._resize(input, self.input_shape) for input in inputs]
        return inputs


class HeadPoseEstimator(Detector, RoiMixin):
    """
    Estimates Head Pose as a Pitch, Roll and Yaw.
    """
    OUTPUT_PITCH = 'angle_p_fc'
    OUTPUT_YAW = 'angle_y_fc'
    OUTPUT_ROLL = 'angle_r_fc'

    def __init__(self, model):
        super(HeadPoseEstimator, self).__init__(model)

        assert len(model.inputs) == 1, "Expected 1 input blob"
        assert len(model.outputs) == 3, "Expected 3 output blobs"
        self.input_blob = next(iter(model.inputs))
        self.input_shape = model.inputs[self.input_blob].shape
        self.name = Detection.HEADPOSE_ESTIMATOR_NAME

        assert np.array_equal([1, 3, 60, 60], self.input_shape), \
            "Expected model input shape %s, but got %s" % \
            ([1, 3, 60, 60], self.input_shape)

        assert np.array_equal([1, 1], model.outputs[self.OUTPUT_PITCH].shape), \
            "Expected '%s' blob output shape %s, got %s" % \
            (self.OUTPUT_PITCH, [1, 1], model.outputs[self.OUTPUT_PITCH].shape)
        assert np.array_equal([1, 1], model.outputs[self.OUTPUT_YAW].shape), \
            "Expected '%s' blob output shape %s, got %s" % \
            (self.OUTPUT_YAW, [1, 1], model.outputs[self.OUTPUT_YAW].shape)
        assert np.array_equal([1, 1], model.outputs[self.OUTPUT_ROLL].shape), \
            "Expected '%s' blob output shape %s, got %s" % \
            (self.OUTPUT_ROLL, [1, 1], model.outputs[self.OUTPUT_ROLL].shape)

    def get_head_poses(self):
        outputs = self.get_outputs()

        results = []
        for output in outputs:
            pitch = output[self.OUTPUT_PITCH][0][0]
            yaw = output[self.OUTPUT_YAW][0][0]
            roll = output[self.OUTPUT_ROLL][0][0]
            results.append(HeadPoseDetectorResult(pitch, yaw, roll))
        return results


class LandmarksDetector(Detector, RoiMixin):
    """
    Detects eyes, nose and lips corners for face.
    """
    POINTS_NUMBER = 5

    def __init__(self, model):
        super(LandmarksDetector, self).__init__(model)

        assert len(model.inputs) == 1, "Expected 1 input blob"
        assert len(model.outputs) == 1, "Expected 1 output blob"
        self.input_blob = next(iter(model.inputs))
        self.output_blob = next(iter(model.outputs))
        self.input_shape = model.inputs[self.input_blob].shape
        self.name = Detection.LANDMARK_DETECTOR_NAME

        assert np.array_equal([1, 3, 48, 48], self.input_shape), \
            "Expected model input shape %s, but got %s" % \
            ([1, 3, 48, 48], self.input_shape)

        assert np.array_equal([1, self.POINTS_NUMBER * 2, 1, 1],
                              model.outputs[self.output_blob].shape), \
            "Expected model output shape %s, but got %s" % \
            ([1, self.POINTS_NUMBER * 2, 1, 1],
             model.outputs[self.output_blob].shape)

    def get_landmarks(self):
        outputs = self.get_outputs()
        results = [LandmarkDetectorResult(out[self.output_blob].reshape((-1, 2))) \
                   for out in outputs]
        return results


class FaceIdentifier(Detector):
    """
    Provides descriptors for faces.
    Depends on landmark detections.
    """
    # Taken from the description of the model:
    # intel_models/face-reidentification-retail-0095
    REFERENCE_LANDMARKS = {
        "left_eye": (30.2946 / 96, 51.6963 / 112),
        "right_eye": (65.5318 / 96, 51.5014 / 112),
        "nose_tip": (48.0252 / 96, 71.7366 / 112),
        "left_lip_corner": (33.5493 / 96, 92.3655 / 112),
        "right_lip_corner": (62.7299 / 96, 92.2041 / 112)
    }

    def __init__(self, model, match_threshold=0.5):
        super(FaceIdentifier, self).__init__(model)

        assert len(model.inputs) == 1, "Expected 1 input blob"
        assert len(model.outputs) == 1, "Expected 1 output blob"

        self.input_blob = next(iter(model.inputs))
        self.output_blob = next(iter(model.outputs))
        self.input_shape = model.inputs[self.input_blob].shape
        self.name = Detection.FACE_IDENTIFIER_NAME

        assert np.array_equal([1, 3, 128, 128], self.input_shape), \
            "Expected model input shape %s, but got %s" % \
            ([1, 3, 128, 128], self.input_shape)

        assert len(model.outputs[self.output_blob].shape) == 4, \
            "Expected model output shape [1, n, 1, 1], got %s" % \
            (model.outputs[self.output_blob].shape)

        self.faces_database = None

        self.match_threshold = match_threshold

    def get_input_shape(self):
        return self.input_shape

    def preprocess(self, frame, rois, landmarks):
        assert len(frame.shape) == 4, "Frame shape should be [1, c, h, w]"
        inputs = cut_rois(frame, rois)
        self._align_rois(inputs, landmarks)
        inputs = [self._resize(input, self.input_shape) for input in inputs]
        return inputs

    def start_async(self, frame, rois, landmarks):
        inputs = self.preprocess(frame, rois, landmarks)
        for input in inputs:
            self.enqueue(input)

    def get_ids(self):
        descriptors = self.get_descriptors()
        return [FaceIdentifierResult(None, None, d) for d in descriptors]

    def get_descriptors(self):
        return [out[self.output_blob].flatten() for out in self.get_outputs()]

    def _normalize(self, array, axis):
        mean = array.mean(axis=axis)
        array -= mean
        std = array.std()
        array /= std
        return mean, std

    def _get_transform(self, src, dst):
        assert np.array_equal(src.shape, dst.shape) and len(src.shape) == 2, \
            "2d input arrays are expected, got %s" % (src.shape)
        src_col_mean, src_col_std = self._normalize(src, axis=(0))
        dst_col_mean, dst_col_std = self._normalize(dst, axis=(0))

        u, _, vt = np.linalg.svd(np.matmul(src.T, dst))
        r = np.matmul(u, vt).T

        transform = np.empty((2, 3))
        transform[:, 0:2] = r * (dst_col_std / src_col_std)
        transform[:, 2] = dst_col_mean.T - \
                          np.matmul(transform[:, 0:2], src_col_mean.T)
        return transform

    def _align_rois(self, face_images, face_landmarks):
        assert len(face_images) == len(face_landmarks), \
            "Input lengths differ, got %s and %s" % \
            (len(face_images), len(face_landmarks))

        for image, image_landmarks in zip(face_images, face_landmarks):
            assert len(image.shape) == 4, "Face image is expected"
            image = image[0]

            scale = np.array((image.shape[-1], image.shape[-2]))
            desired_landmarks = np.array([
                self.REFERENCE_LANDMARKS["left_eye"],
                self.REFERENCE_LANDMARKS["right_eye"],
                self.REFERENCE_LANDMARKS["nose_tip"],
                self.REFERENCE_LANDMARKS["left_lip_corner"],
                self.REFERENCE_LANDMARKS["right_lip_corner"],
            ], dtype=np.float64) * scale

            landmarks = np.array([
                image_landmarks.left_eye,
                image_landmarks.right_eye,
                image_landmarks.nose_tip,
                image_landmarks.left_lip_corner,
                image_landmarks.right_lip_corner,
            ], dtype=np.float64) * scale

            transform = self._get_transform(desired_landmarks, landmarks)
            img = image.transpose((1, 2, 0))
            cv2.warpAffine(img, transform, tuple(scale), img,
                           flags=cv2.WARP_INVERSE_MAP)
            image[:] = img.transpose((2, 0, 1))


class AgeGenderEstimator(Detector, RoiMixin):
    """
    Estimates age as a real value and gender as a probability of being male.
    """
    _AGE_LAYER = 'age_conv3'
    _GENDER_LAYER = 'prob'

    def __init__(self, model):
        super(AgeGenderEstimator, self).__init__(model)

        assert len(model.inputs) == 1, "Expected 1 input blob"
        assert len(model.outputs) == 2, "Expected 2 output blob"
        self.input_blob = next(iter(model.inputs))
        self.output_blob = next(iter(model.outputs))
        self.input_shape = model.inputs[self.input_blob].shape
        self.name = Detection.AGE_GENDER_ESTIMATOR_NAME

    def get_age_gender(self):
        outputs = self.get_outputs()
        results = [AgeGenderResult(
            int(100*out[self._AGE_LAYER].flatten()[0]),
            float(out[self._GENDER_LAYER].reshape(2,)[1])
        ) for out in outputs]
        return results


class EmotionEstimator(Detector, RoiMixin):
    """
    Estimates facial emotions as probs distributed at 5 possible outcomes:
    neutral, happy, sad, surprise, anger.
    """
    _EMOTION_LAYER = 'prob_emotion'

    def __init__(self, model):
        super(EmotionEstimator, self).__init__(model)

        assert len(model.inputs) == 1, "Expected 1 input blob"
        assert len(model.outputs) == 1, "Expected 1 output blob"
        self.input_blob = next(iter(model.inputs))
        self.output_blob = next(iter(model.outputs))
        self.input_shape = model.inputs[self.input_blob].shape
        self.name = Detection.EMOTION_ESTIMATOR_NAME

    def get_emotions(self):
        outputs = self.get_outputs()
        emotions = [x[self._EMOTION_LAYER].flatten() for x in outputs]
        results = [EmotionEstimatorResult(em) for em in emotions]
        return results
