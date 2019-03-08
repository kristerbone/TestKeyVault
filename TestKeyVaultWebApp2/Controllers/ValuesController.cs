using System;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestKeyVaultWebApp2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public async Task<ActionResult<string>> Get()
        {
            await OnGetAsync();
            return Message;
        }

        private string keyVaultName = "VNetSecureTestKV";

        public string Message { get; set; }

        public async Task OnGetAsync()
        {
            Message = "Your application description page.";
            int retries = 0;
            bool retry = false;
            try
            {
                /* The next four lines of code show you how to use AppAuthentication library to fetch secrets from your key vault*/
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var secret = await keyVaultClient.GetSecretAsync($"https://{keyVaultName}.vault.azure.net/secrets/TestSecret").ConfigureAwait(false);
                Message = secret.Value;

                /* The following *do while* logic is to handle throttling errors thrown by Azure Key Vault.
                   It shows how to do exponential backoff, which is the recommended client side throttling*/
                do
                {
                    long waitTime = Math.Min(getWaitTime(retries), 2000000);
                    secret = await keyVaultClient.GetSecretAsync($"https://{keyVaultName}.vault.azure.net/secrets/AppSecret").ConfigureAwait(false);
                    retry = false;
                }
                while (retry && (retries++ < 10));
            }
            catch (KeyVaultErrorException keyVaultException)
            {
                Message = keyVaultException.Message;
                if ((int)keyVaultException.Response.StatusCode == 429)
                    retry = true;
            }
        }

        // This method implements exponential backoff if there are 429 errors from Azure Key Vault
        private static long getWaitTime(int retryCount)
        {
            long waitTime = ((long)Math.Pow(2, retryCount) * 100L);
            return waitTime;
        }

        // This method fetches a token from Azure Active Directory, which can then be provided to Azure Key Vault to authenticate
        public async Task<string> GetAccessTokenAsync()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            return accessToken;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
