using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text;
using PgpCore;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace PGP_Encryption_Azure_Function_Docker
{
    public class PGPEncryptBlobTriggerFunction
    {
        [FunctionName("PGPEncryptBlobTriggerFunction")]
        public static async Task RunAsync([BlobTrigger("%pgpencodesourcecontainername%/{name}", Connection = "pgpEncodeSourceStorage")]Stream myBlob,
                               [Blob("%pgpencodetargetcontainername%/{name}.pgp", FileAccess.Write, Connection = "pgpEncodeTargetStorage")]Stream outBlob,
                               string name, 
	                       ILogger log)
        {

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("pgpPublicKeyVendor")) || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("pgpPublicKey"))){
                
                log.LogError("Public keys are null or empty.");  
            }else{
            
                string publicKeyVendor = Encoding.UTF8.GetString(Convert.FromBase64String(Environment.GetEnvironmentVariable("pgpPublicKeyVendor")));
                string publicKey = Encoding.UTF8.GetString(Convert.FromBase64String(Environment.GetEnvironmentVariable("pgpPublicKey")));
                
                try{
                    log.LogInformation($"Encrypting file: {name} using vendor's public key");
                    Stream encryptedData = await EncryptAsync(myBlob, outBlob, publicKeyVendor, publicKey);
                    log.LogInformation($"Successfully encryped file: {name}");
                    log.LogInformation($"Moving encryped file: {name}.pgp to target destination");
                    await encryptedData.CopyToAsync(outBlob);
                    log.LogInformation($"Successfully moved encryped file: {name}.pgp to destination");
                }catch (PgpException pgpException){
                    log.LogError(pgpException.Message);    
                }
            }
        }

        private static async Task<Stream> EncryptAsync(Stream inputStream, Stream outputStreamTemp, string publicKeyVendor, string publicKey){
        
            System.Collections.Generic.List<Stream> publicKeyStreams = new System.Collections.Generic.List<Stream>();

            using (inputStream)
            using (Stream publicKeyVendorStream = GenerateStreamFromString(publicKeyVendor))
            using (Stream publicKeyStream = GenerateStreamFromString(publicKey))
                {
                    Stream memStream = new MemoryStream();
                    publicKeyStreams.Add(publicKeyVendorStream);
                    publicKeyStreams.Add(publicKeyStream);    
                    EncryptionKeys encryptionKeys = new EncryptionKeys(publicKeyStreams);
                    using (PGP pgp = new PGP(encryptionKeys)){
                        await pgp.EncryptStreamAsync(inputStream, memStream, true, true);
                        memStream.Seek(0, SeekOrigin.Begin);
                        return memStream;
                    }
                }
        }
        
        private static Stream GenerateStreamFromString(string s){
            
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }   
    }    
}
