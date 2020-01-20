#!/usr/bin/env bash
docker build -t heedbookcontainerregistry.azurecr.io/faceanalyzeservice:latest -f FaceAnalyzeService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/userservice:latest -f UserService.Dockerfile . && 
docker build -t heedbookcontainerregistry.azurecr.io/useroperations:latest -f UserOperations.Dockerfile . && 
docker build -t heedbookcontainerregistry.azurecr.io/fillingframeservice:latest -f FillingFrameService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/extractframesfromvideoservice:latest -f ExtractFramesFromVideoService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/videotosoundservice:latest -f VideoToSoundService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/audioanalyzeservice:latest -f AudioAnalyzeService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/toneanalyzeservice:latest -f ToneAnalyzeService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/audioanalyzescheduler:latest -f AudioAnalyzeScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/dialoguestatuscheckerscheduler:latest -f DialogueStatusCheckerScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/dialoguevideomergeservice:latest -f DialogueVideoMergeService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/fillinghintservice:latest -f FillingHintService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/fillingsatisfactionservice:latest -f FillingSatisfactionService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/dialoguevideoassembleservice:latest -f DialogueVideoAssembleService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/dialoguemarkupservice:latest -f DialogueMarkUpService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/sessionstatusscheduler:latest -f SessionStatusScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/onlinetuiofficesscheduler:latest -f OnlineTuiOfficesScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/heedbookdevelopmentstatisticsscheduler:latest -f HeedbookDevelopmentStatistics.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/dialoguesrecalculatescheduler:latest -f DialoguesRecalculateScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/senduseranalyticreportscheduler:latest -f SendUserAnalyticReportScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/dialogueandsessionsnestedscheduler:latest -f DialogueAndSessionsNestedScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/oldvideotoframeextractscheduler:latest -f OldVideoToFrameExtractScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/logsaveservice:latest -f LogSaveService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/messengerreporterservice:latest -f MessengerReporterService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/cloneftponazureservice:latest -f CloneFtpOnAzureService.Dockerfile . &&
docker build -t heedbookcontainerregistry.azurecr.io/deleteoldlogsonelasticscheduler:latest -f DeleteOldLogsOnElasticScheduler.Dockerfile .

docker push heedbookcontainerregistry.azurecr.io/faceanalyzeservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/userservice:latest && 
docker push heedbookcontainerregistry.azurecr.io/useroperations:latest && 
docker push heedbookcontainerregistry.azurecr.io/fillingframeservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/extractframesfromvideoservice:latest && 
docker push heedbookcontainerregistry.azurecr.io/videotosoundservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/audioanalyzeservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/toneanalyzeservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/dialoguestatuscheckerscheduler:latest &&
docker push heedbookcontainerregistry.azurecr.io/audioanalyzescheduler:latest &&
docker push heedbookcontainerregistry.azurecr.io/dialoguevideomergeservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/fillinghintservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/fillingsatisfactionservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/dialoguevideoassembleservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/dialoguemarkupservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/sessionstatusscheduler:latest &&
docker push heedbookcontainerregistry.azurecr.io/onlinetuiofficesscheduler:latest &&
docker push heedbookcontainerregistry.azurecr.io/heedbookdevelopmentstatisticsscheduler:latest &&
docker push heedbookcontainerregistry.azurecr.io/dialoguesrecalculatescheduler:latest &&
docker push heedbookcontainerregistry.azurecr.io/senduseranalyticreportscheduler:latest &&
docker push heedbookcontainerregistry.azurecr.io/dialogueandsessionsnestedscheduler:latest &&
docker push heedbookcontainerregistry.azurecr.io/oldvideotoframeextractscheduler:latest &&
docker push heedbookcontainerregistry.azurecr.io/logsaveservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/messengerreporterservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/cloneftponazureservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/deleteoldlogsonelasticscheduler:latest
