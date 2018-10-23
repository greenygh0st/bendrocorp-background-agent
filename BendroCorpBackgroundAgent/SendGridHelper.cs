using System;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BendroCorpBackgroundAgent
{
    public class SendGridHelper
    {
        const string outro = "<p><b><strong>Please do not reply to this email.</strong></b><p/><p>Sincerely,</p><p>Kaden Aayhan<br />Assistant to the CEO<br />BendroCorp, Inc.</p><p>Corp Plaza 11, Platform R3Q<br />Crusader, Stanton</p>";
        private string Reciever { get; set; }
        private string Email { get; set; }
        private string Subject { get; set; }
        private string Message { get; set; }

        public async Task<bool> Send()
        {
            try
            {
                var myMessage = new SendGridMessage();
                myMessage.AddTo(Email, Reciever);
                myMessage.From = new EmailAddress("no-reply@bendrocorp.com", "BendroCorp");
                myMessage.Subject = Subject;
                myMessage.HtmlContent = Message + outro;

                var transportWeb = new SendGrid.SendGridClient(Environment.GetEnvironmentVariable("SENDGRID_API_KEY"));//
                Response resp = await transportWeb.SendEmailAsync(myMessage);//.Wait();
                if (resp.StatusCode == System.Net.HttpStatusCode.OK || resp.StatusCode == System.Net.HttpStatusCode.Created || resp.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    return true;
                }else{
                    throw new Exception("Error Occured: Email could not be sent! " + resp.Body);
                }

            }
            catch (Exception)
            {
                return false;
            }
        }

        public SendGridHelper(string reciever, string email, string subject, string message)
        {
            Reciever = reciever;
            Email = email;
            Subject = subject;
            Message = message;
        }
    }
}
