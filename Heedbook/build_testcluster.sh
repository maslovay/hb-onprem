#!/usr/bin/env bash

sh ./build_prepare_testcluster.sh

docker build -t hbtestregistry.azurecr.io/faceanalyzeservice:latesttest -f tmp/FaceAnalyzeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/userservice:latesttest -f tmp/UserService.Dockerfile . && 
docker build -t hbtestregistry.azurecr.io/useroperations:latesttest -f tmp/UserOperations.Dockerfile . && 
docker build -t hbtestregistry.azurecr.io/fillingframeservice:latesttest -f tmp/FillingFrameService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/extractframesfromvideoservice:latesttest -f tmp/ExtractFramesFromVideoService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/videotosoundservice:latesttest -f tmp/VideoToSoundService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/audioanalyzeservice:latesttest -f tmp/AudioAnalyzeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/toneanalyzeservice:latesttest -f tmp/ToneAnalyzeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/audioanalyzescheduler:latesttest -f tmp/AudioAnalyzeScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguestatuscheckerscheduler:latesttest -f tmp/DialogueStatusCheckerScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguevideomergeservice:latesttest -f tmp/DialogueVideoMergeService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/fillinghintservice:latesttest -f tmp/FillingHintService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/fillingsatisfactionservice:latesttest -f tmp/FillingSatisfactionService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguevideoassembleservice:latesttest -f tmp/DialogueVideoAssembleService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguemarkupservice:latesttest -f tmp/DialogueMarkUpService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/sessionstatusscheduler:latesttest -f tmp/SessionStatusScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/onlinetuiofficesscheduler:latesttest -f tmp/OnlineTuiOfficesScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/heedbookdevelopmentstatisticsscheduler:latesttest -f tmp/HeedbookDevelopmentStatistics.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/dialoguesrecalculatescheduler:latesttest -f tmp/DialoguesRecalculateScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/senduseranalyticreportscheduler:latesttest -f tmp/SendUserAnalyticReportScheduler.Dockerfile .&&
docker build -t hbtestregistry.azurecr.io/referencecontroller:latesttest -f tmp/ReferenceController.Dockerfile .&&
docker build -t hbtestregistry.azurecr.io/dialogueandsessionsnestedscheduler:latest -f DialogueAndSessionsNestedScheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/oldvideotoframeextractsheduler:latest -f OldVideoToFrameExtractSheduler.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/logsaveservice:latest -f LogSaveService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/messengerreporterservice:latest -f MessengerReporterService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/cloneftponazureservice:latest -f CloneFtpOnAzureService.Dockerfile . &&
docker build -t hbtestregistry.azurecr.io/deleteoldlogsonelasticscheduler:latest -f DeleteOldLogsOnElasticScheduler.Dockerfile .


docker push hbtestregistry.azurecr.io/faceanalyzeservice:latesttest &&
docker push hbtestregistry.azurecr.io/userservice:latesttest && 
docker push hbtestregistry.azurecr.io/useroperations:latesttest && 
docker push hbtestregistry.azurecr.io/fillingframeservice:latesttest &&
docker push hbtestregistry.azurecr.io/extractframesfromvideoservice:latesttest && 
docker push hbtestregistry.azurecr.io/videotosoundservice:latesttest &&
docker push hbtestregistry.azurecr.io/audioanalyzeservice:latesttest &&
docker push hbtestregistry.azurecr.io/toneanalyzeservice:latesttest &&
docker push hbtestregistry.azurecr.io/dialoguestatuscheckerscheduler:latesttest &&
docker push hbtestregistry.azurecr.io/audioanalyzescheduler:latesttest &&
docker push hbtestregistry.azurecr.io/dialoguevideomergeservice:latesttest &&
docker push hbtestregistry.azurecr.io/fillinghintservice:latesttest &&
docker push hbtestregistry.azurecr.io/fillingsatisfactionservice:latesttest &&
docker push hbtestregistry.azurecr.io/dialoguevideoassembleservice:latesttest &&
docker push hbtestregistry.azurecr.io/dialoguemarkupservice:latesttest &&
docker push hbtestregistry.azurecr.io/sessionstatusscheduler:latesttest &&
docker push hbtestregistry.azurecr.io/onlinetuiofficesscheduler:latesttest &&
docker push hbtestregistry.azurecr.io/heedbookdevelopmentstatisticsscheduler:latesttest &&
docker push hbtestregistry.azurecr.io/dialoguesrecalculatescheduler:latesttest &&
docker push hbtestregistry.azurecr.io/senduseranalyticreportscheduler:latesttest &&
docker push hbtestregistry.azurecr.io/referencecontroller:latesttest &&
docker push hbtestregistry.azurecr.io/dialogueandsessionsnestedscheduler:latest &&
docker push hbtestregistry.azurecr.io/oldvideotoframeextractsheduler:latest &&
docker push hbtestregistry.azurecr.io/logsaveservice:latest &&
docker push hbtestregistry.azurecr.io/messengerreporterservice:latest &&
docker push hbtestregistry.azurecr.io/cloneftponazureservice:latest &&
docker push hbtestregistry.azurecr.io/deleteoldlogsonelasticscheduler:latest

rm -rf tmp
