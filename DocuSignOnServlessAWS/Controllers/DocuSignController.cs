using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using DocuSign.eSign.Api;
using DocuSign.eSign.Model;
using DocuSignOnServlessAWS.Models;

namespace DocuSignOnServlessAWS.Controllers
{
    public class DocuSignController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public DocuSignController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }


        [HttpGet]
        public ActionResult SendDocumentEmailToSign()
        {
            //set default values
            var model = new ElectornicConsentRequestViewModel
            {
                SenderUsername = "vitaltrax.test@gmail.com",
                SenderPassword = "vitaltrax#2017",
                SenderIntegratorKey = "e90f0740-f3d6-487e-812e-9ac856d5b1dc"
            };
            return View("SendDocumentEmailToSign", model);
        }

        [HttpPost]
        public async Task<ActionResult> SendDocumentEmailToSign(ElectornicConsentRequestViewModel model)
        {
            try
            {
                if (Request.Form.Files != null)
                {
                    //Get upload file. Sav to temp path
                    var docFile = Request.Form.Files[0];
                    //byte[] binary = new byte[docFile.ContentLength];
                    //docFile.InputStream.Read(binary, 0, docFile.ContentLength);

                    string fileSavedPath = Path.Combine(_hostingEnvironment.WebRootPath, "Temp", docFile.FileName);

                    using (var ms = new MemoryStream())
                    {
                        using (var stream = docFile.OpenReadStream())
                        {
                            await stream.CopyToAsync(ms);
                        }
                        using (var file = new FileStream(fileSavedPath, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            ms.WriteTo(file);
                        }
                    }

                    // Send  letter and view shat result will receive Recipient================
                    model.SignTest1File = fileSavedPath;

                    model.RecipientDocumentPreview = SendDocuSignLetter(model.SenderUsername, model.SenderPassword, model.SenderIntegratorKey,
                                                                    model.RecipientName, model.RecipientEmail, model.SignTest1File);

                    // Remove temp file
                    if(System.IO.File.Exists(fileSavedPath))
                        System.IO.File.Delete(fileSavedPath);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            return View("SendDocumentEmailToSign", model);
        }

        /// <summary>
        /// (for production change to www.docusign.net/restapi)
        /// </summary>
        public const string basePath = "https://demo.docusign.net/restapi";

        static LoginInformation loginInfo { get; set; }

        /// <summary>
        /// Init DocuSign ApiClient
        /// </summary>
        /// <param name="senderUsername"></param>
        /// <param name="senderPassword"></param>
        /// <returns></returns>
        public static LoginInformation GetDocuSignApiClient(string senderUsername, string senderPassword, string senderIntegratorKey)
        {
            //bool loggedIn = loginInfo.LoginAccounts.Where(a => a.UserName == senderUsername && a.ApiPassword == senderPassword)
            if (loginInfo == null)
            {
                var apiClient = new DocuSign.eSign.Client.ApiClient(basePath);
                // set client in global config so we don't need to pass it to each API object
                DocuSign.eSign.Client.Configuration.Default.ApiClient = apiClient;
                string authHeader = "{\"Username\":\"" + senderUsername + "\", \"Password\":\"" + senderPassword + "\", \"IntegratorKey\":\"" + senderIntegratorKey + "\"}";
                DocuSign.eSign.Client.Configuration.Default.AddDefaultHeader("X-DocuSign-Authentication", authHeader);

                //2 the authentication api uses the apiClient (and X-DocuSign-Authentication header) that are set in Configuration object
                AuthenticationApi authApi = new AuthenticationApi();
                loginInfo = authApi.Login();
            }
            //else
            //    loginInfo.LoginAccounts.Add();

            return loginInfo;
        }

        /// <summary>
        /// Method for sending ducument "Terms and Agreent" to be signed.
        /// returns View of the recepient (what he will see)
        /// <summary>
        /// 1.1 create test account on https://account-d.docusign.com/password (to be added intead [Username],[Password], [IntegratorKey])
        /// 1.2 generate integrator key (see https://admindemo.docusign.com/api-integrator-key and how https://support.docusign.com/en/answers/00003890)
        /// 2. install NuGet packages, for example with PM> install-package [pkgName]
        /// 2.1. PM> install-package DocuSign
        /// 2.2. PM> install-package RestSharp
        /// 2.3. PM> install-package Newtonsoft.Json
        /// NOTE: for dev https://appdemo.docusign.com/home, https://www.docusign.com/developer-center/recipes/request-a-signature-via-email
        ///3.1. Enter sender DocuSign credentials: senderUsername, senderPassword, senderIntegratorKey
        ///NOTE:  Create a TEST account on https://account-d.docusign.com/password 
        ///and generate IntegratorKey. See https://admindemo.docusign.com/api-integrator-key (guide how https://support.docusign.com/en/answers/00003890)
        ///3.2. specify the document we want signed
        /// SignTest1File = @"[PATH/TO/DOCUMENT/TEST.PDF]";
        ///3.3. specify (signer) recipientName and recipientEmail
        private ViewUrl SendDocuSignLetter(string senderUsername, string senderPassword, string senderIntegratorKey = "1d38a73a-fe59-4679-b1dc-58bce8064e80",
                                       string recipientName = "TEST RECEIVER", string recipientEmail = "",
                                       string SignTest1File = "")
        {

            //4. Init api client with appropriate environment (for production change to www.docusign.net/restapi)
            var loginInfo = GetDocuSignApiClient(senderUsername, senderPassword, senderIntegratorKey);

            // we will retrieve this from the login() results
            //NOTE: user might be a member of multiple accounts
            string accountId = loginInfo.LoginAccounts[0].AccountId;//"2417016";

            //Console.WriteLine("LoginInformation: {0}", loginInfo.ToJson());

            // Read a file from disk to use as a document
            byte[] fileBytes = System.IO.File.ReadAllBytes(SignTest1File);

            EnvelopeDefinition envDef = new EnvelopeDefinition();
            envDef.EmailSubject = "Please sign this doc";

            //2 Add a document to the envelope
            Document doc = new Document();
            doc.DocumentBase64 = System.Convert.ToBase64String(fileBytes);
            doc.Name = Path.GetFileName(SignTest1File);
            doc.DocumentId = "1";

            envDef.Documents = new List<Document>();
            envDef.Documents.Add(doc);

            //3 Add a recipient to sign the documeent
            Signer signer = new Signer();
            signer.Name = recipientName;
            signer.Email = recipientEmail;
            signer.RecipientId = "1";

            // must set |clientUserId| to embed the recipient
            signer.ClientUserId = "1234"; //TODO:

            // Create a |SignHere| tab somewhere on the document for the recipient to sign
            signer.Tabs = new Tabs();
            signer.Tabs.SignHereTabs = new List<SignHere>();
            SignHere signHere = new SignHere();
            signHere.DocumentId = "1"; //TODO: can be 2, 3, etc.
            signHere.PageNumber = "1"; //page can be 1,2,...
            signHere.RecipientId = "1";
            signHere.XPosition = "100";
            signHere.YPosition = "150";
            signer.Tabs.SignHereTabs.Add(signHere);

            envDef.Recipients = new Recipients();
            envDef.Recipients.Signers = new List<Signer>();
            envDef.Recipients.Signers.Add(signer);

            // set envelope status to "sent" to immediately send the signature request
            envDef.Status = "sent";

            //4 Create and send the signature request with EnvelopesApi
            EnvelopesApi envelopesApi = new EnvelopesApi();
            EnvelopeSummary envelopeSummary = envelopesApi.CreateEnvelope(accountId, envDef);
            var summary = JsonConvert.SerializeObject(envelopeSummary);
            //envelopeSummary.StatusDateTime
            //envelopeSummary.Status

            //5. To SEE WHAT RECEPIENT will see
            RecipientViewRequest viewOptions = new RecipientViewRequest()
            {
                ReturnUrl = "https://www.docusign.com/devcenter",
                ClientUserId = "1234",  // must match clientUserId set in step #2!
                AuthenticationMethod = "email",
                UserName = recipientName,
                Email = recipientEmail
            };

            // Create the recipient view (like signing URL)
            ViewUrl recipientView = envelopesApi.CreateRecipientView(accountId, envelopeSummary.EnvelopeId, viewOptions);

            // Start the embedded signing session!
            //System.Diagnostics.Process.Start(recipientView.Url);

            return recipientView;
        }
    }
}