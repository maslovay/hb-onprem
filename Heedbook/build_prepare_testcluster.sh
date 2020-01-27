rm -rf tmp
mkdir tmp
cp *.Dockerfile* tmp

sed -i "s/testcluster/true/" tmp/FaceAnalyzeService.Dockerfile  
sed -i "s/testcluster/true/" tmp/UserService.Dockerfile 
sed -i "s/testcluster/true/" tmp/UserOperations.Dockerfile 
sed -i "s/testcluster/true/" tmp/FillingFrameService.Dockerfile 
sed -i "s/testcluster/true/" tmp/ExtractFramesFromVideoService.Dockerfile 
sed -i "s/testcluster/true/" tmp/VideoToSoundService.Dockerfile 
sed -i "s/testcluster/true/" tmp/AudioAnalyzeService.Dockerfile 
sed -i "s/testcluster/true/" tmp/ToneAnalyzeService.Dockerfile 
sed -i "s/testcluster/true/" tmp/AudioAnalyzeScheduler.Dockerfile 
sed -i "s/testcluster/true/" tmp/DialogueStatusCheckerScheduler.Dockerfile 
sed -i "s/testcluster/true/" tmp/DialogueVideoMergeService.Dockerfile 
sed -i "s/testcluster/true/" tmp/FillingHintService.Dockerfile 
sed -i "s/testcluster/true/" tmp/FillingSatisfactionService.Dockerfile 
sed -i "s/testcluster/true/" tmp/DialogueVideoAssembleService.Dockerfile 
sed -i "s/testcluster/true/" tmp/DialogueMarkUpService.Dockerfile 
sed -i "s/testcluster/true/" tmp/SessionStatusScheduler.Dockerfile 
sed -i "s/testcluster/true/" tmp/OnlineTuiOfficesScheduler.Dockerfile 
sed -i "s/testcluster/true/" tmp/HeedbookDevelopmentStatistics.Dockerfile 
sed -i "s/testcluster/true/" tmp/DialoguesRecalculateScheduler.Dockerfile 
sed -i "s/testcluster/true/" tmp/SendUserAnalyticReportScheduler.Dockerfile 
sed -i "s/testcluster/true/" tmp/ReferenceController.Dockerfile 
sed -i "s/testcluster/true/" tmp/DialogueAndSessionsNestedScheduler.Dockerfile
sed -i "s/testcluster/true/" tmp/OldVideoToFrameExtractScheduler.Dockerfile
sed -i "s/testcluster/true/" tmp/LogSaveService.Dockerfile
sed -i "s/testcluster/true/" tmp/MessengerReporterService.Dockerfile
sed -i "s/testcluster/true/" tmp/CloneFtpOnAzureService.Dockerfile
sed -i "s/testcluster/true/" tmp/DeleteOldLogsOnElasticScheduler.Dockerfile
sed -i "s/testcluster/true/" tmp/IntegrationAPITestsService.Dockerfile
