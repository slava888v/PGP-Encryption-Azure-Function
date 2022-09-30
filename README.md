# PGP Encrypt with Blob-Triggered Azure Function

## Summary
This is a simple Blob Trigger Azure Function, which PGP encrypts storage container files.
The code is reliant on [PGPCore](https://github.com/mattosaurus/PgpCore) .NET Core class library for encryption. 
The function is binded with the source and target containers on two seperate storage accounts. When a new or updated file is detected on the source container, the function wakes up, PGP encrypts the file then places it in the target container. 

The files are PGP encrypted using two public keys (sender's and receiver's).

<!-- **Note:** This is not a production code and was written for research purposes only. -->


## Building and running in Docker (locally)
Tested on OS X 12.6

### Asumptions

* Visual Studio for Mac 7.5 and higher is installed.
* .NET Core 3.1 SDK is isntalled
* Docker is installed

### Building Docker Image

After checking out the repo, in terminal, cd to the project root directory

Build Docker image using the `Dockerfile` file

```console
docker build -f ./Dockerfile -t blob-triggered-encrypt-docker-image .
```

### Running Docker Image

The following parameters have to be passed to the docker image when running:

* pgpencodesourcecontainername="<_name of the source container. The PGP encryption function will be called on any new or updated files in this container_>" 
* pgpEncodeSourceStorage="<_source storage connection string_>"
* pgpencodetargetcontainername="<_name of the target container. PGP Encrypted files will be saved into this container_>"
* pgpEncodeTargetStorage="<_target storage connection string_>"
* AzureWebJobsStorage="<_same as pgpEncodeSourceStorage. Blob trigger function requires this value as a default for binding to a container_>"
* pgpPublicKey="<_sender's public key_>"
* pgpPublicKeyVendor="<_receiver's public key_>"


```console

docker run --name blob-triggered-encrypt-docker-container -p 80:80 -it \
-e pgpencodesourcecontainername="" \
-e pgpEncodeSourceStorage="" \
-e pgpencodetargetcontainername="" \
-e pgpEncodeTargetStorage="" \
-e AzureWebJobsStorage="" \
-e pgpPublicKey="" \
-e pgpPublicKeyVendor=""  blob-triggered-encrypt-docker-image
```

Upload any file to the container on the source storage account in Azure. The PGP encrypted version of that file should appear in the container on the target storage account.

To stop the container
```console
docker stop blob-triggered-encrypt-docker-container
```

## Building and running on Azure

### Asumptions

#### The following services are provisioned on Azure

    * Container registry (for Docker images)
    * App Service Plan
    * Function App with Storage Account
    * Key Vault
    * 2 Storage Accounts (source and target)
    * Application Insights (optional)

#### Configuration

    * Key Vault has the following secrets: pgpPublicKey, pgpPublicKeyVendor. The the values of all keys should be Base64 encoded (see NOTES section at the bottom if you need help with this)
    * Function App is using System Assigned Identity
    * Key Store's Access Control has special permission for the Function App to read Key Vault's secrets (Key Vault Secret User)
    * Function App has the following Application Settings:
        
        - pgpencodesourcecontainername
        - pgpEncodeSourceStorage
        - pgpencodetargetcontainername
        - pgpEncodeTargetStorage
        - AzureWebJobsStorage
        - pgpPublicKey
        - pgpPublicKeyVendor
    
    * pgpPublicKey and pgpPublicKeyVendor Application Settings have values pointing to the secrets in Key Vault using the following format: @Microsoft.KeyVault(SecretUri=Secret Identifier). i.e: @Microsoft.KeyVault(SecretUri=https://KEY_VAULT_NAME.vault.azure.net/secrets/SECRET_NAME/SECRET_VERSION)


After checking out the repo, in terminal, cd to the project root directory

1. Build Docker image using the `Dockerfile` file

```console
docker build -f ./Dockerfile -t blob-triggered-encrypt-docker-image .
```
    
2. Push newly created Docker image (blob-triggered-encrypt-docker-image) to Container Registry on Azure.

3. Deploy the Docker Image on Function App by selecting blob-triggered-encrypt-docker-image Container Registry Docker image in the Deployment Center

4. Start the Function App.

5. Upload a file to the container on the source storage account. The PGP encrypted version of that file should appear in the container on the target storage account.
  
    

## NOTES

1. The encryption is using both public keys: sender's and receiver's. This is not necessary and was just a requirement for this project. The code can be easily amended to use receiver's public key only for encryption.

2. If you want to quickly run the Function App without worrying about  generating PGP key pair, use the following resources:

    - [Free Online PGP Key Generator](https://pgptool.org/) 

    - [Base64 Encoder](https://base64.guru/converter) 





