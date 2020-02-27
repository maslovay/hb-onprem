#!/usr/bin/env bash
docker build -t heedbookcontainerregistrytest.azurecr.io/faceanalyzeservice:latest -f FaceAnalyzeService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/userservice:latest -f UserService.Dockerfile . && 
docker build -t heedbookcontainerregistrytest.azurecr.io/useroperations:latest -f UserOperations.Dockerfile . && 
docker build -t heedbookcontainerregistrytest.azurecr.io/fillingframeservice:latest -f FillingFrameService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/extractframesfromvideoservice:latest -f ExtractFramesFromVideoService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/videotosoundservice:latest -f VideoToSoundService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/audioanalyzeservice:latest -f AudioAnalyzeService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/toneanalyzeservice:latest -f ToneAnalyzeService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/audioanalyzescheduler:latest -f AudioAnalyzeScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/dialoguestatuscheckerscheduler:latest -f DialogueStatusCheckerScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/dialoguevideomergeservice:latest -f DialogueVideoMergeService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/fillinghintservice:latest -f FillingHintService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/fillingsatisfactionservice:latest -f FillingSatisfactionService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/dialoguevideoassembleservice:latest -f DialogueVideoAssembleService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/dialoguemarkupservice:latest -f DialogueMarkUpService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/sessionstatusscheduler:latest -f SessionStatusScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/onlinetuiofficesscheduler:latest -f OnlineTuiOfficesScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/heedbookdevelopmentstatisticsscheduler:latest -f HeedbookDevelopmentStatistics.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/dialoguesrecalculatescheduler:latest -f DialoguesRecalculateScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/senduseranalyticreportscheduler:latest -f SendUserAnalyticReportScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/dialogueandsessionsnestedscheduler:latest -f DialogueAndSessionsNestedScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/oldvideotoframecutsheduler:latest -f OldVideoToFrameCutSheduler.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/logsaveservice:latest -f LogSaveService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/messengerreporterservice:latest -f MessengerReporterService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/cloneftponazureservice:latest -f CloneFtpOnAzureService.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/deleteoldlogsonelasticscheduler:latest -f DeleteOldLogsOnElasticScheduler.Dockerfile . &&
docker build -t heedbookcontainerregistrytest.azurecr.io/unitapitestsservice:latest -f UnitAPITestsService.Dockerfile .


docker push heedbookcontainerregistrytest.azurecr.io/faceanalyzeservice:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/userservice:latest && 
docker push heedbookcontainerregistrytest.azurecr.io/useroperations:latest && 
docker push heedbookcontainerregistrytest.azurecr.io/fillingframeservice:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/extractframesfromvideoservice:latest && 
docker push heedbookcontainerregistrytest.azurecr.io/videotosoundservice:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/audioanalyzeservice:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/toneanalyzeservice:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/dialoguestatuscheckerscheduler:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/audioanalyzescheduler:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/dialoguevideomergeservice:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/fillinghintservice:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/fillingsatisfactionservice:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/dialoguevideoassembleservice:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/dialoguemarkupservice:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/sessionstatusscheduler:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/onlinetuiofficesscheduler:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/heedbookdevelopmentstatisticsscheduler:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/dialoguesrecalculatescheduler:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/senduseranalyticreportscheduler:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/dialogueandsessionsnestedscheduler:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/oldvideotoframecutsheduler:latest &&
docker push heedbookcontainerregistrytest.azurecr.io/logsaveservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/messengerreporterservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/cloneftponazureservice:latest &&
docker push heedbookcontainerregistry.azurecr.io/deleteoldlogsonelasticscheduler:latest &&
docker push heedbookcontainerregistry.azurecr.io/unitapitestsservice:latest
