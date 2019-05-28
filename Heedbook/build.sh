docker build -t containerregistryhb.azurecr.io/faceanalyzeservice:latest -f FaceAnalyzeService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/userservice:latest -f UserService.Dockerfile . && 
docker build -t containerregistryhb.azurecr.io/useroperations:latest -f UserOperations.Dockerfile . && 
docker build -t containerregistryhb.azurecr.io/fillingframeservice:latest -f FillingFrameService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/extractframesfromvideoservice:latest -f ExtractFramesFromVideoService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/videotosoundservice:latest -f VideoToSoundService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/audioanalyzeservice:latest -f AudioAnalyzeService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/audioanalyzescheduler:latest -f AudioAnalyzeScheduler.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/dialoguestatuscheckerscheduler:latest -f DialogueStatusCheckerScheduler.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/dialoguevideomergeservice:latest -f DialogueVideoMergeService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/fillinghintservice:latest -f FillingHintService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/fillingsatisfactionservice:latest -f FillingSatisfactionService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/dialoguevideoassembleservice:latest -f DialogueVideoAssembleService.Dockerfile . &&
docker build -t containerregistryhb.azurecr.io/dialoguemarkup:latest -f DialogueMarkUpService.Dockerfile .
                                                               
docker push containerregistryhb.azurecr.io/faceanalyzeservice:latest &&
docker push containerregistryhb.azurecr.io/userservice:latest && 
docker push containerregistryhb.azurecr.io/useroperations:latest && 
docker push containerregistryhb.azurecr.io/fillingframeservice:latest &&
docker push containerregistryhb.azurecr.io/extractframesfromvideoservice:latest && 
docker push containerregistryhb.azurecr.io/videotosoundservice:latest &&
docker push containerregistryhb.azurecr.io/audioanalyzeservice:latest &&
docker push containerregistryhb.azurecr.io/dialoguestatuscheckerscheduler:latest &&
docker push containerregistryhb.azurecr.io/audioanalyzescheduler:latest &&
docker push containerregistryhb.azurecr.io/dialoguevideomergeservice:latest &&
docker push containerregistryhb.azurecr.io/fillinghintservice:latest &&
docker push containerregistryhb.azurecr.io/fillingsatisfactionservice:latest &&
docker push containerregistryhb.azurecr.io/dialoguevideoassembleservice:latest &&
docker push containerregistryhb.azurecr.io/dialoguemarkup:latest
