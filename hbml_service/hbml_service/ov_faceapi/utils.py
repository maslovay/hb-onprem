import logging as log
import os
from argparse import ArgumentParser
from os import path as osp

import cv2
import numpy as np
from numpy import clip
from openvino.inference_engine import IEPlugin
from scipy.optimize import linear_sum_assignment
from scipy.spatial.distance import cosine

from .detections import FaceDetectorResult

DEVICE_KINDS = ['CPU', 'GPU', 'FPGA', 'MYRIAD', 'HETERO']


def build_argparser():
    parser = ArgumentParser()

    general = parser.add_argument_group('General')
    general.add_argument('-i', '--input', metavar="PATH", default='cam',
                         help="(optional) Path to the input video " \
                              "('cam' for the camera, default)")
    general.add_argument('-o', '--output', metavar="PATH", default="",
                         help="(optional) Path to save the output video to")
    general.add_argument('-no_show', action='store_true',
                         help="(optional) Do not display output")
    general.add_argument('-tl', '--timelapse', action='store_true',
                         help="(optional) Auto-pause after each frame")
    general.add_argument('-cw', '--crop_width', default=0, type=int,
                         help="(optional) Crop the input stream to this width")
    general.add_argument('-ch', '--crop_height', default=0, type=int,
                         help="(optional) Crop the input stream to this height")

    gallery = parser.add_argument_group('Faces database')
    gallery.add_argument('-fg', metavar="PATH",
                         help="Path to the face images directory")
    gallery.add_argument('--run_detector', action='store_true',
                         help="(optional) Use Face Detection model to find faces" \
                              " on the face images, otherwise use full images.")

    models = parser.add_argument_group('Models')
    models.add_argument('-m_fd', metavar="PATH", default="", required=True,
                        help="Path to the Face Detection Adas or Retail model XML file")
    models.add_argument('-m_lm', metavar="PATH", default="", required=True,
                        help="Path to the Facial Landmarks Regression Retail model XML file")
    models.add_argument('-m_reid', metavar="PATH", default="", required=True,
                        help="Path to the Face Reidentification Retail model XML file")
    models.add_argument('-m_hp', metavar="PATH", default="", required=True,
                        help="Path to the Head Pose Estimation Retail model XML file")
    models.add_argument('-m_ag', metavar="PATH", default="", required=True,
                        help="Path to the Age Gender Estimation model XML file")
    models.add_argument('-m_em', metavar="PATH", default="", required=True,
                        help="Path to the Emotion Estimation model XML file")

    infer = parser.add_argument_group('Inference options')
    infer.add_argument('-d_fd', default='CPU', choices=DEVICE_KINDS,
                       help="(optional) Target device for the " \
                            "Face Detection Retail model " \
                            "(default: %(default)s)")
    infer.add_argument('-d_lm', default='CPU', choices=DEVICE_KINDS,
                       help="(optional) Target device for the " \
                            "Facial Landmarks Regression Retail model " \
                            "(default: %(default)s)")
    infer.add_argument('-d_reid', default='CPU', choices=DEVICE_KINDS,
                       help="(optional) Target device for the " \
                            "Face Reidentification Retail model " \
                            "(default: %(default)s)")
    infer.add_argument('-d_hp', default='CPU', choices=DEVICE_KINDS,
                       help="(optional) Target device for the " \
                            "Head Pose Estimation Retail model " \
                            "(default: %(default)s)")
    infer.add_argument('-d_ag', default='CPU', choices=DEVICE_KINDS,
                       help="(optional) Target device for the " \
                            "Age Gender Estimation model " \
                            "(default: %(default)s)")
    infer.add_argument('-d_em', default='CPU', choices=DEVICE_KINDS,
                       help="(optional) Target device for the " \
                            "Emotion Estimation Retail model " \
                            "(default: %(default)s)")
    infer.add_argument('-l', '--cpu_lib', metavar="PATH", default="",
                       help="(optional) For MKLDNN (CPU)-targeted custom layers, if any. " \
                            "Path to a shared library with custom layers " \
                            "implementations")
    infer.add_argument('-c', '--gpu_lib', metavar="PATH", default="",
                       help="(optional) For clDNN (GPU)-targeted custom layers, if any. " \
                            "Path to the XML file with descriptions of the kernels")
    infer.add_argument('-v', '--verbose', action='store_true',
                       help="(optional) Be more verbose")
    infer.add_argument('-pc', '--perf_stats', action='store_true',
                       help="(optional) Output detailed per-layer performance stats")
    infer.add_argument('-t_fd', metavar='[0..1]', type=float, default=0.6,
                       help="(optional) Probability threshold for face detections" \
                            "(default: %(default)s)")
    infer.add_argument('-t_id', metavar='[0..1]', type=float, default=0.3,
                       help="(optional) Cosine distance threshold between two vectors " \
                            "for face identification " \
                            "(default: %(default)s)")
    infer.add_argument('-exp_r_fd', metavar='NUMBER', type=float, default=1.15,
                       help="(optional) Scaling ratio for bboxes passed to face recognition " \
                            "(default: %(default)s)")
    return parser


class InferenceContext:
    def __init__(self):
        self.plugins = {}

    def load_plugins(self, devices, cpu_ext="", gpu_ext=""):
        log.info("Loading plugins for devices: %s" % (devices))

        plugins = {d: IEPlugin(d) for d in devices}
        if 'CPU' in plugins and not len(cpu_ext) == 0:
            log.info("Using CPU extensions library '%s'" % (cpu_ext))
            assert osp.isfile(cpu_ext), "Failed to open CPU extensions library"
            plugins['CPU'].add_cpu_extension(cpu_ext)

        if 'GPU' in plugins and not len(gpu_ext) == 0:
            assert osp.isfile(gpu_ext), "Failed to open GPU definitions file"
            plugins['GPU'].set_config({"CONFIG_FILE": gpu_ext})

        self.plugins = plugins

        log.info("Plugins are loaded")

    def get_plugin(self, device):
        return self.plugins.get(device, None)

    def check_model_support(self, net, device):
        plugin = self.plugins[device]

        if plugin.device == "CPU":
            supported_layers = plugin.get_supported_layers(net)
            not_supported_layers = [l for l in net.layers.keys() \
                                    if l not in supported_layers]
            if len(not_supported_layers) != 0:
                log.error("The following layers are not supported " \
                          "by the plugin for the specified device {}:\n {}". \
                          format(plugin.device, ', '.join(not_supported_layers)))
                log.error("Please try to specify cpu extensions " \
                          "library path in the command line parameters using " \
                          "the '-l' parameter")
                raise NotImplementedError(
                    "Some layers are not supported on the device")

    def deploy_model(self, model, device, max_requests=1):
        self.check_model_support(model, device)
        plugin = self.plugins[device]
        deployed_model = plugin.load(network=model, num_requests=max_requests)
        return deployed_model


class FacesDatabase:
    IMAGE_EXTENSIONS = ['.jpg', '.png']

    class Identity:
        def __init__(self, label, descriptor):
            self.label = label
            self.descriptor = descriptor

    def __init__(self, path,
                 face_identifier, landmarks_detector, face_detector=None):
        path = osp.abspath(path)
        paths = []
        if osp.isdir(path):
            ext = self.IMAGE_EXTENSIONS
            paths = [osp.join(path, f) for f in os.listdir(path) \
                     if f.endswith(ext[0]) or f.endswith(ext[1])]
        else:
            raise Exception("Wrong face images database path. Expected a " \
                            "path to the directory containing %s files, " \
                            "but got '%s'" % \
                            (" or ".join(self.IMAGE_EXTENSIONS), path))

        if len(paths) == 0:
            raise Exception("The images database folder has no images")

        self.database = []
        for path in paths:
            label = osp.splitext(osp.basename(path))[0]
            image = cv2.imread(path, flags=cv2.IMREAD_COLOR)

            assert len(image.shape) == 3, \
                "Expected an input image in (H, W, C) format"
            assert image.shape[2] in [3, 4], \
                "Expected BGR or BGRA input"

            if image.shape[0] == 4:  # assume BGRA
                image = image[:, :, :3]
            image = image.transpose((2, 0, 1))  # HWC to CHW
            image = np.expand_dims(image, axis=0)

            if face_detector:
                face_detector.start_async(image)
                rois = face_detector.get_roi_proposals(image)
                if len(rois) < 1:
                    log.warning("Not found faces on the image '%s'" % (path))

                    w, h = image.shape[-1], image.shape[-2]
                    rois = [FaceDetectorResult([0, 0, 0, 0, 0, w, h])]
            else:
                w, h = image.shape[-1], image.shape[-2]
                rois = [FaceDetectorResult([0, 0, 0, 0, 0, w, h])]

            for i, roi in enumerate(rois):
                r = [roi]
                landmarks_detector.start_async(image, r)
                landmarks = landmarks_detector.get_landmarks()

                face_identifier.start_async(image, r, landmarks)
                descriptor = face_identifier.get_descriptors()[0]

                self.database.append(
                    self.Identity("%s-%s" % (label, i), descriptor))

    def match_faces(self, descriptors):
        database = self
        distances = np.empty((len(descriptors), len(database)))
        for i, desc in enumerate(descriptors):
            for j, identity in enumerate(database):
                distances[i][j] = self.cosine_dist(desc, identity.descriptor)

        # Find best assignments, prevent repeats, assuming faces can not repeat
        _, assignments = linear_sum_assignment(distances)
        matches = []
        for i in range(len(descriptors)):
            if len(assignments) <= i:  # assignment failure, too many faces
                matches.append((0, 1.0))
                continue

            id = assignments[i]
            distance = distances[i, id]
            matches.append((id, distance))
        return matches

    def __getitem__(self, idx):
        return self.database[idx]

    def __len__(self):
        return len(self.database)

    def cosine_dist(self, x, y):
        return cosine(x, y) * 0.5


def cut_roi(frame, roi):
    p1 = roi.position.astype(int)
    p1 = clip(p1, [0, 0], [frame.shape[-1], frame.shape[-2]])
    p2 = (roi.position + roi.size).astype(int)
    p2 = clip(p2, [0, 0], [frame.shape[-1], frame.shape[-2]])
    return np.array(frame[:, :, p1[1]:p2[1], p1[0]:p2[0]])


def cut_rois(frame, rois):
    return [cut_roi(frame, roi) for roi in rois]


def center_crop(frame, crop_size):
    fh, fw, fc = frame.shape
    crop_size[0] = min(fw, crop_size[0])
    crop_size[1] = min(fh, crop_size[1])
    return frame[(fh - crop_size[1]) // 2: (fh + crop_size[1]) // 2,
            (fw - crop_size[0]) // 2: (fw + crop_size[0]) // 2,
            :]


def deprecated(original):
    def modified(*args, **kwargs):
        log.warning("Function '{}' is deprecated".format(original.__name__))
        return original(*args, **kwargs)
    return modified


def build_config(**kwargs):
    parser = build_argparser()
    pseudo_args = []
    for k, v in kwargs.items():
        pseudo_args.append(str(k))
        pseudo_args.append(str(v))
    namespace = parser.parse_args(pseudo_args)
    for (k,v) in kwargs.items():
        if hasattr(namespace, k):
            setattr(namespace, k, v)
    return vars(namespace)
