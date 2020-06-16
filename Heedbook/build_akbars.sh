#!/usr/bin/env bash
docker build -t heedbook/faceanalyzeservice:latest -f FaceAnalyzeService.Dockerfile . &&
docker build -t heedbook/fillingframeservice:latest -f FillingFrameService.Dockerfile . &&
docker build -t heedbook/extractframesfromvideoservice:latest -f ExtractFramesFromVideoService.Dockerfile . &&
docker build -t heedbook/videotosoundservice:latest -f VideoToSoundService.Dockerfile . &&
docker build -t heedbook/audioanalyzeservice:latest -f AudioAnalyzeService.Dockerfile . &&
docker build -t heedbook/toneanalyzeservice:latest -f ToneAnalyzeService.Dockerfile . &&
docker build -t heedbook/audioanalyzescheduler:latest -f AudioAnalyzeScheduler.Dockerfile . &&
docker build -t heedbook/dialoguestatuscheckerscheduler:latest -f DialogueStatusCheckerScheduler.Dockerfile . &&
docker build -t heedbook/dialoguevideoassembleservice:latest -f DialogueVideoAssembleService.Dockerfile . &&
docker build -t heedbook/fillingsatisfactionservice:latest -f FillingSatisfactionService.Dockerfile . &&
docker build -t heedbook/dialoguevideoassembleservice:latest -f DialogueVideoAssembleService.Dockerfile . &&
docker build -t heedbook/detectfaceidextendedscheduler:latest -f DetectFaceIdExtendedScheduler.Dockerfile . &&
docker build -t heedbook/dialoguecreator -f DialogueCreatorScheduler.Dockerfile . &&
docker build -t heedbook/persondetectionservice -f PersonDetectionService.Dockerfile . &&
docker build -t heedbook/persononlinedetectionservice -f PersonOnlineDetectionService.Dockerfile .
docker build -t heedbook/useroperations -f UserOperations.Dockerfile . &&
docker build -t heedbook/userservice -f UserService.Dockerfile .

