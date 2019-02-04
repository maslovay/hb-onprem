docker build -t heedbookregistry.azurecr.io/heedbookdev/faceanalyzeservice:latest -f FaceAnalyzeService.Dockerfile . &&
docker build -t heedbookregistry.azurecr.io/heedbookdev/operationservice:latest -f OperationService.Dockerfile . &&
docker build -t heedbookregistry.azurecr.io/heedbookdev/userservice:latest -f UserService.Dockerfile . && 
docker build -t heedbookregistry.azurecr.io/heedbookdev/framefromvideoservice:latest -f FrameFromVideoService.Dockerfile . && 
docker build -t heedbookregistry.azurecr.io/heedbookdev/fillingframeservice:latest -f FillingFrameService.Dockerfile . &&
cd ExtractFramesFromVideo &&
docker build -t heedbookregistry.azurecr.io/heedbookdev/extractfromframesservice:latest . &&

docker push heedbookregistry.azurecr.io/heedbookdev/cognitiveservice:latest &&
docker push heedbookregistry.azurecr.io/heedbookdev/operationservice:latest &&
docker push heedbookregistry.azurecr.io/heedbookdev/userservice:latest && 
docker push heedbookregistry.azurecr.io/heedbookdev/framefromvideoservice:latest &&
docker push heedbookregistry.azurecr.io/heedbookdev/extractfromframesservice:latest && 
docker push heedbookregistry.azurecr.io/heedbookdev/fillingframeservice:latest
