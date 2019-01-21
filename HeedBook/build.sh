docker build -t cognitiveservice -f CognitiveService.Dockerfile . &&
docker build -t operationservice -f OperationService.Dockerfile . &&
docker build -t userservice -f UserService.Dockerfile . && 
docker build -t framefromvideoservice -f FrameFromVideoService.Dockerfile . && 

docker tag cognitiveservice heedbookregistry.azurecr.io/heedbookdev/cognitiveservice:latest &&
docker tag operationservice heedbookregistry.azurecr.io/heedbookdev/operationservice:latest &&
docker tag userservice heedbookregistry.azurecr.io/heedbookdev/userservice:latest &&
docker tag framefromvideoservice heedbookregistry.azurecr.io/heedbookdev/framefromvideoservice:latest &&

docker push heedbookregistry.azurecr.io/heedbookdev/cognitiveservice:latest &&
docker push heedbookregistry.azurecr.io/heedbookdev/operationservice:latest &&
docker push heedbookregistry.azurecr.io/heedbookdev/userservice:latest && 
docker push heedbookregistry.azurecr.io/heedbookdev/framefromvideoservice:latest
