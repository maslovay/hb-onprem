import numpy as np
import logging
from time import time
from .processors import FrameProcessor
from .utils import center_crop
from .detections import Detection


class FaceApiModule:
    """
    Provides interface for face api inference.
    config is a dict with keys defined in utils.build_config
    """
    def __init__(self, config):
        self.frame_processor = FrameProcessor(config)

        self.input_crop = None
        if config['crop_width'] and config['crop_height']:
            self.input_crop = np.array((config['crop_width'], config['crop_height']))

    def detect(self, frame, excluded_fields=tuple()):
        """
        Executes inference on image. excluded_fields won't be listed
        in Detections
        """
        # TODO: не делать инференс значений в excluded_fields
        if self.input_crop is not None:
            frame = center_crop(frame, self.input_crop)
        start_time = time()
        detections = self.frame_processor.process(frame)
        logging.info("Total inference ended, took {:1.4f}s".format(time() - start_time))
        return Detection.build_from_results(detections, excluded_fields)

    def get_performance_stats(self):
        return self.frame_processor.get_performance_stats()
