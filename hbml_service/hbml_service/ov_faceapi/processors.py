import logging as log
from os import path as osp

import numpy as np
from openvino.inference_engine import IENetwork

from .detectors import FaceDetector, HeadPoseEstimator, LandmarksDetector, FaceIdentifier, AgeGenderEstimator, EmotionEstimator
from .detections import Detection
from .utils import InferenceContext


class FrameProcessor:
    QUEUE_SIZE = 16

    def __init__(self, args):
        used_devices = {args['d_fd'], args['d_hp'], args['d_lm'], args['d_reid'], args['d_ag']}
        self.context = InferenceContext()
        context = self.context
        context.load_plugins(used_devices, args['cpu_lib'], args['gpu_lib'])
        for d in used_devices:
            context.get_plugin(d).set_config({
                "PERF_COUNT": "YES" if args['perf_stats'] else "NO"})

        log.info("Loading models")
        face_detector_net = self.load_model(args['m_fd'])
        head_pose_net = self.load_model(args['m_hp'])
        landmarks_net = self.load_model(args['m_lm'])
        face_reid_net = self.load_model(args['m_reid'])
        age_gender_net = self.load_model(args['m_ag'])
        emotion_net = self.load_model(args['m_em'])

        self.face_detector = FaceDetector(face_detector_net,
                                          confidence_threshold=args['t_fd'], roi_scale_factor=args['exp_r_fd'])
        self.head_pose_estimator = HeadPoseEstimator(head_pose_net)
        self.landmarks_detector = LandmarksDetector(landmarks_net)
        self.face_identifier = FaceIdentifier(face_reid_net,
                                              match_threshold=args['t_id'])
        self.age_gender_estimator = AgeGenderEstimator(age_gender_net)
        self.emotion_estimator = EmotionEstimator(emotion_net)

        self.face_detector.deploy(args['d_fd'], context)
        self.head_pose_estimator.deploy(args['d_hp'], context,
                                        queue_size=self.QUEUE_SIZE)
        self.landmarks_detector.deploy(args['d_lm'], context,
                                       queue_size=self.QUEUE_SIZE)
        self.face_identifier.deploy(args['d_reid'], context,
                                    queue_size=self.QUEUE_SIZE)
        self.age_gender_estimator.deploy(args['d_ag'], context,
                                         queue_size=self.QUEUE_SIZE)
        self.emotion_estimator.deploy(args['d_em'], context,
                                         queue_size=self.QUEUE_SIZE)
        log.info("Models are loaded")

    def load_model(self, model_path):
        model_path = osp.abspath(model_path)
        model_description_path = model_path
        model_weights_path = osp.splitext(model_path)[0] + ".bin"
        log.info("Loading the model from '%s'" % (model_description_path))
        assert osp.isfile(model_description_path), \
            "Model description is not found at '%s'" % (model_description_path)
        assert osp.isfile(model_weights_path), \
            "Model weights are not found at '%s'" % (model_weights_path)
        model = IENetwork(model_description_path, model_weights_path)
        log.info("Model is loaded")
        return model

    def process(self, frame):
        assert len(frame.shape) == 3, \
            "Expected input frame in (H, W, C) format"
        assert frame.shape[2] in [3, 4], \
            "Expected BGR or BGRA input"

        if frame.shape[0] == 4:  # assume BGRA
            frame = frame[:, :, :3]
        frame = frame.transpose((2, 0, 1))  # HWC to CHW
        frame = np.expand_dims(frame, axis=0)

        self.face_detector.clear()
        self.head_pose_estimator.clear()
        self.landmarks_detector.clear()
        self.face_identifier.clear()

        self.face_detector.start_async(frame)
        rois = self.face_detector.get_roi_proposals(frame)
        if self.QUEUE_SIZE < len(rois):
            log.warning("Too many faces for processing." \
                        " Will be processed only %s of %s." % \
                        (self.QUEUE_SIZE, len(rois))
                        )
            rois = rois[:self.QUEUE_SIZE]
        self.head_pose_estimator.start_async(frame, rois)
        self.landmarks_detector.start_async(frame, rois)
        self.age_gender_estimator.start_async(frame, rois)
        self.emotion_estimator.start_async(frame, rois)

        landmarks = self.landmarks_detector.get_landmarks()
        self.face_identifier.start_async(frame, rois, landmarks)

        head_poses = self.head_pose_estimator.get_head_poses()
        age_gender = self.age_gender_estimator.get_age_gender()
        face_identities = self.face_identifier.get_ids()
        emotions = self.emotion_estimator.get_emotions()

        outputs = {}
        outputs[self.face_detector.name] = rois
        outputs[self.head_pose_estimator.name] = head_poses
        outputs[self.landmarks_detector.name] = landmarks
        outputs[self.face_identifier.name] = face_identities
        outputs[self.age_gender_estimator.name] = age_gender
        outputs[self.emotion_estimator.name] = emotions
        return outputs

    def get_performance_stats(self):
        stats = {
            Detection.FACE_DETECTOR_NAME: self.face_detector.get_performance_stats(),
            Detection.HEADPOSE_ESTIMATOR_NAME: self.head_pose_estimator.get_performance_stats(),
            Detection.LANDMARK_DETECTOR_NAME: self.landmarks_detector.get_performance_stats(),
            Detection.FACE_IDENTIFIER_NAME: self.face_identifier.get_performance_stats(),
            Detection.EMOTION_ESTIMATOR_NAME: self.emotion_estimator.get_performance_stats(),
            Detection.AGE_GENDER_ESTIMATOR_NAME: self.age_gender_estimator.get_performance_stats()
        }
        return stats
