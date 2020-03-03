import numpy as np
import cv2


class DetectionsVisualizer:
    DEFAULT_CAMERA_FOCAL_DISTANCE = 950.0
    COLOR1 = (255, 255, 255)
    COLOR2 = (0, 220, 0)

    def draw_text_with_background(
            self, frame, text, origin,
            font=cv2.FONT_HERSHEY_SIMPLEX, scale=1.0,
            color=(0, 0, 0), thickness=1, bgcolor=COLOR1
    ):
        #TODO: запустить для многострочных текстов
        text_size, baseline = cv2.getTextSize(text, font, scale, thickness)
        cv2.rectangle(
            frame,
            tuple((origin + (0, baseline)).astype(int)),
            tuple((origin + (text_size[0], -text_size[1])).astype(int)),
            bgcolor, cv2.FILLED
        )

        cv2.putText(
            frame, text,
            tuple(origin.astype(int)),
            font, scale, color, thickness)
        return frame

    def draw_rectangle(self, frame, position, size):
        cv2.rectangle(frame, tuple(position), tuple(position + size), self.COLOR2, 2)

    def draw_detection(self, frame, detection):
        fd = detection[detection.FACE_DETECTOR_NAME]
        if not len(fd):
            return
        position = np.array((fd['xc'], fd['yc'])).astype(int)
        size = np.array([fd['w'], fd['h']]).astype(int)
        text = str(detection[detection.AGE_GENDER_ESTIMATOR_NAME])
        text += str(detection[detection.EMOTION_ESTIMATOR_NAME])
        self.draw_text_with_background(frame, text, position)
        self.draw_rectangle(frame, position, size)

    def get_labeled_frame(self, frame, detections):
        labeled_frame = frame.copy()
        for d in detections:
            self.draw_detection(labeled_frame, d)
        return labeled_frame
