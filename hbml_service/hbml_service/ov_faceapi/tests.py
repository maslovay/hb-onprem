import cv2
import logging
import sys
from .module import FaceApiModule, Detection
from .utils import build_config
from .visual import DetectionsVisualizer


config = {
    '-m_lm': '/opt/intel/computer_vision_sdk/deployment_tools/intel_models/landmarks-regression-retail-0009/FP32/landmarks-regression-retail-0009.xml',
    '-m_hp': '/opt/intel/computer_vision_sdk/deployment_tools/intel_models/head-pose-estimation-adas-0001/FP32/head-pose-estimation-adas-0001.xml',
    '-m_fd': '/opt/intel/computer_vision_sdk/deployment_tools/intel_models/face-detection-retail-0004/FP32/face-detection-retail-0004.xml',
    '-m_reid': '/opt/intel/computer_vision_sdk/deployment_tools/intel_models/face-reidentification-retail-0095/FP32/face-reidentification-retail-0095.xml',
    '-m_ag': '/opt/intel/computer_vision_sdk/deployment_tools/intel_models/age-gender-recognition-retail-0013/FP32/age-gender-recognition-retail-0013.xml',
    '-m_em': '/opt/intel/computer_vision_sdk_2018.5.455/deployment_tools/intel_models/emotions-recognition-retail-0003/FP32/emotions-recognition-retail-0003.xml',
    '-l': '/home/vagrant/inference_engine_samples/intel64/Release/lib/libcpu_extension.so'
}


def logs2stdout():
    root = logging.getLogger()
    root.setLevel(logging.DEBUG)

    handler = logging.StreamHandler(sys.stdout)
    handler.setLevel(logging.DEBUG)
    formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
    handler.setFormatter(formatter)
    root.addHandler(handler)


def run_mdl(input, output):
    logs2stdout()
    cnf = build_config(**config)
    mdl = FaceApiModule(cnf)
    img = cv2.imread(input)
    dts = mdl.detect(img, excluded_fields=[Detection.FACE_IDENTIFIER_NAME])
    vis = DetectionsVisualizer()
    img_labeled = vis.get_labeled_frame(img, dts)
    cv2.imwrite(output, img_labeled)
