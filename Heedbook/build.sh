#!/usr/bin/env bash
docker build -t containerregistryhb.azurecr.io/faceanalyzeservice:latest -f FaceAnalyzeService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/userservice:latest -f UserService.Dockerfile . && 
docker build -t containerregistryhb.azurecr.io/useroperations:latest -f UserOperations.Dockerfile . && 
docker build -t containerregistryhb.azurecr.io/fillingframeservice:latest -f FillingFrameService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/extractframesfromvideoservice:latest -f ExtractFramesFromVideoService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/videotosoundservice:latest -f VideoToSoundService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/audioanalyzeservice:latest -f AudioAnalyzeService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/toneanalyzeservice:latest -f ToneAnalyzeService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/audioanalyzescheduler:latest -f AudioAnalyzeScheduler.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/dialoguestatuscheckerscheduler:latest -f DialogueStatusCheckerScheduler.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/fillinghintservice:latest -f FillingHintService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/fillingsatisfactionservice:latest -f FillingSatisfactionService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/dialoguevideoassembleservice:latest -f DialogueVideoAssembleService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/dialoguemarkupservice:latest -f DialogueMarkUpService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/sessionstatusscheduler:latest -f SessionStatusScheduler.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/persondetectionservice:latest -f PersonDetectionService.Dockerfile . && 

docker save  containerregistryhb.azurecr.io/faceanalyzeservice:latest > faceanalyze &&
docker save  containerregistryhb.azurecr.io/userservice:latest > userservice  && 
docker save  containerregistryhb.azurecr.io/useroperations:latest > useroperations  && 
docker save  containerregistryhb.azurecr.io/fillingframeservice:latest > fillingframe  &&
docker save  containerregistryhb.azurecr.io/extractframesfromvideoservice:latest > extractframes &&
docker save  containerregistryhb.azurecr.io/videotosoundservice:latest > videotosound &&
docker save  containerregistryhb.azurecr.io/audioanalyzeservice:latest > audioanalyze &&
docker save  containerregistryhb.azurecr.io/toneanalyzeservice:latest > toneanalyze &&
docker save  containerregistryhb.azurecr.io/audioanalyzescheduler:latest > audioanalyzescheduler &&
docker save  containerregistryhb.azurecr.io/dialoguestatuscheckerscheduler:latest > dialoguestatuschecker &&
docker save  containerregistryhb.azurecr.io/fillinghintservice:latest > fillinghints &&
docker save  containerregistryhb.azurecr.io/fillingsatisfactionservice:latest > fillingsatisfaction &&
docker save  containerregistryhb.azurecr.io/dialoguevideoassembleservice:latest > dialoguevideoassemble &&
docker save  containerregistryhb.azurecr.io/dialoguemarkupservice:latest > dialoguemarkup &&
docker save  containerregistryhb.azurecr.io/sessionstatusscheduler:latest > sessionstatus &&
docker save  containerregistryhb.azurecr.io/persondetectionservice:latest > persondetection 
