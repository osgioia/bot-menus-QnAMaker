#!/bin/bash

APP_NAME=bot-adopcion-msteams-$CLIENTE-prod
RESOURCE_GROUP=rg-prod-Sally-southbr
SUBSCRIPTION_ID=259fbe6e-d5f6-445c-be5e-7461f7195c28
CONTAINER_NAME="imagenes-$APP_NAME"
STORAGE_ACCOUNT=botdepot

MIN_CHARACTERS=4
MAX_CHARACTERS=15
CANT_PARAMETERS=1

if [[ "$#" -eq "$CANT_PARAMETERS" && $1 =~ ^[a-zA-Z0-9\-]+$ && "${#1}" -ge "$MIN_CHARACTERS" && "${#1}" -le "$MAX_CHARACTERS" ]]
then
  CLIENTE=$1

  az account set --subscription $SUBSCRIPTION_ID
  #echo "Seleccionada la subscription con id: $SUBSCRIPTION_ID"

  APP_ID=$(az ad app create --display-name "Bot Adopcion MS Teams - $CLIENTE" --password "AtLeastSixteenCharacters_0" --available-to-other-tenants --query 'appId' -o tsv)
  #echo "MicrosoftAppId: $APP_ID"

  PASSWORD=$(az ad app credential reset --id $APP_ID --append  --query 'password' -o tsv)
  #echo "MicrosoftAppPassword: $PASSWORD"

  BLOB_URL="https://$STORAGE_ACCOUNT.blob.core.windows.net/$CONTAINER_NAME"

  az storage container create --resource-group "$RESOURCE_GROUP" --account-name "$STORAGE_ACCOUNT" --name "$CONTAINER_NAME" --auth-mode login

  az deployment group create  --resource-group "$RESOURCE_GROUP" \
                              --template-file ".\DeploymentTemplates\template-with-preexisting-rg.json" \
                              --parameters  appId="$APP_ID" \
                                            appSecret="$PASSWORD" \
                                            botId="$APP_NAME" \
                                            newWebAppName="$APP_NAME" \
                                            existingAppServicePlan="Logicalis-Sally" \
                                            appServicePlanLocation="Brazil South" \
                              --name "$APP_NAME"

  az bot prepare-deploy --lang Csharp --code-dir "." --proj-file-path "QnABot.csproj"

  #TODO: Completar valores en appsettigs.json automaticamente y generar el zip
  #zip -r bot-adopcion-msteams.zip ./

  echo "Editar el archivo appsetting.json y completar con los siguientes valores"
  echo "MicrosoftAppId: $APP_ID"
  echo "MicrosoftAppPassword: $PASSWORD"
  echo "BlobImagesUrl: $BLOB_URL"
  echo "-----------------------------------------------------------------"
  echo "Nombre App service: $APP_NAME"
  echo "Nombre Blob Container: $CONTAINER_NAME"
  read -p "Una vez generado el zip presione enter para continuar"

  az webapp deployment source config-zip --resource-group "$RESOURCE_GROUP" \
                                         --name "$APP_NAME" \
                                         --src ".\bot-adopcion-msteams.zip"
else
  echo "Pasar el nombre del CLIENTE sin espacios y con un máximo de 15 caracteres alfanumerocos o -"
fi