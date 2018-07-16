using System.ComponentModel.DataAnnotations;


namespace DocuSignOnServlessAWS.Models
{
    public class ElectornicConsentRequestViewModel
    {
        [Required]
        public string SenderUsername { get; set; }

        [Required]
        public string SenderPassword { get; set; }

        [Required]
        public string SenderIntegratorKey { get; set; }

        public string RecipientName { get; set; }

        [Required]
        public string RecipientEmail { get; set; }

        public string SignTest1File { get; set; }

        /// <summary>
        /// what Recipient will see in the letter
        /// </summary>
        public DocuSign.eSign.Model.ViewUrl RecipientDocumentPreview { get; set; }
        
    }
 }