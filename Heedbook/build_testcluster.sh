#!/usr/bin/env bash

sh ./build_prepare_testcluster.sh

docker build -t hbtestregistry.azurecr.io/faceanalyzeservice:latest -f tmp/FaceAnalyzeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/userservice:latest -f tmp/UserService.Dockerfile . && 
docker build -t hbtestregistry.azurecr.io/useroperations:latest -f tmp/UserOperations.Dockerfile . && 
docker build -t hbtestregistry.azurecr.io/fillingframeservice:latest -f tmp/FillingFrameService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/extractframesfromvideoservice:latest -f tmp/ExtractFramesFromVideoService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/videotosoundservice:latest -f tmp/VideoToSoundService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/audioanalyzeservice:latest -f tmp/AudioAnalyzeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/toneanalyzeservice:latest -f tmp/ToneAnalyzeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/audioanalyzescheduler:latest -f tmp/AudioAnalyzeScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguestatuscheckerscheduler:latest -f tmp/DialogueStatusCheckerScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguevideomergeservice:latest -f tmp/DialogueVideoMergeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/fillinghintservice:latest -f tmp/FillingHintService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/fillingsatisfactionservice:latest -f tmp/FillingSatisfactionService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguevideoassembleservice:latest -f tmp/DialogueVideoAssembleService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguemarkupservice:latest -f tmp/DialogueMarkUpService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/sessionstatusscheduler:latest -f tmp/SessionStatusScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/onlinetuiofficesscheduler:latest -f tmp/OnlineTuiOfficesScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/heedbookdevelopmentstatisticsscheduler:latest -f tmp/HeedbookDevelopmentStatistics.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguesrecalculatescheduler:latest -f tmp/DialoguesRecalculateScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/senduseranalyticreportscheduler:latest -f tmp/SendUserAnalyticReportScheduler.Dockerfile .&&
docker build -t hbtestregistry.azurecr.io/referencecontroller:latest -f tmp/ReferenceController.Dockerfile .&&

docker push hbtestregistry.azurecr.io/faceanalyzeservice:latest &&
docker push hbtestregistry.azurecr.io/userservice:latest && 
docker push hbtestregistry.azurecr.io/useroperations:latest && 
docker push hbtestregistry.azurecr.io/fillingframeservice:latest &&
docker push hbtestregistry.azurecr.io/extractframesfromvideoservice:latest && 
docker push hbtestregistry.azurecr.io/videotosoundservice:latest &&
docker push hbtestregistry.azurecr.io/audioanalyzeservice:latest &&
docker push hbtestregistry.azurecr.io/toneanalyzeservice:latest &&
docker push hbtestregistry.azurecr.io/dialoguestatuscheckerscheduler:latest &&
docker push hbtestregistry.azurecr.io/audioanalyzescheduler:latest &&
docker push hbtestregistry.azurecr.io/dialoguevideomergeservice:latest &&
docker push hbtestregistry.azurecr.io/fillinghintservice:latest &&
docker push hbtestregistry.azurecr.io/fillingsatisfactionservice:latest &&
docker push hbtestregistry.azurecr.io/dialoguevideoassembleservice:latest &&
docker push hbtestregistry.azurecr.io/dialoguemarkupservice:latest &&
docker push hbtestregistry.azurecr.io/sessionstatusscheduler:latest &&
docker push hbtestregistry.azurecr.io/onlinetuiofficesscheduler:latest &&
docker push hbtestregistry.azurecr.io/heedbookdevelopmentstatisticsscheduler:latest &&
docker push hbtestregistry.azurecr.io/dialoguesrecalculatescheduler:latest &&
docker push hbtestregistry.azurecr.io/senduseranalyticreportscheduler:latest &&
docker push hbtestregistry.azurecr.io/referencecontroller:latest 

rm -rf tmp