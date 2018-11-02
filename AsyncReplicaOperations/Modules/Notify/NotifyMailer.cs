using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace AsyncReplicaOperations
{
    public class NotifyMailer:IDisposable
    {
        private static NotifyMailer instance;
        private SmtpClient mailClient;
        private string templateMessage;

        public static NotifyMailer getInstance()
        {
            if(instance == null)
            {
                instance = new NotifyMailer();
            }
            return instance;
        }

        [ParameterMethod("Host")]
        private NotifyMailer()
        {
            mailClient = new SmtpClient();
            mailClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
            mailClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            mailClient.Host = OperationsAPI.Host;
            //mailClient.Port = Properties.Settings.Default.Port;
        }

        public string MessageBody
        {
            get
            {
                return templateMessage;
            }
            set
            {
                templateMessage = value;
            }
        }

        [ParameterMethod("SenderMail")]
        public void sendMail(List<string> mailRecepients,List<string> mailCopyRecepients, Dictionary<string, string> parametersMap, string subject = "Выгрузка реплик",MailPriority priority = MailPriority.Normal)
        {
            var message = new MailMessage();
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            message.Priority = priority;
            message.Subject = subject;
            message.Sender = new MailAddress(OperationsAPI.SenderMail);
            message.From = message.Sender;

            var recepEnum = mailRecepients.GetEnumerator();

            while (recepEnum.MoveNext())
            {
                message.To.Add(recepEnum.Current);
            }

            var copyRecepEnum = mailCopyRecepients.GetEnumerator();
            
            while(copyRecepEnum.MoveNext())
            {
                message.CC.Add(copyRecepEnum.Current);
            }

            var messageBody = templateMessage;

            var paramEnum = parametersMap.GetEnumerator();

            while(paramEnum.MoveNext())
            {
                messageBody.Replace(paramEnum.Current.Key, paramEnum.Current.Value);
            }

            message.Body = messageBody;

            mailClient.Send(message);
        }

        [ParameterMethod("SenderMail")]
        public void sendMail(List<string> mailRecepients, List<string> mailCopyRecepients, string subject = "Выгрузка реплик", MailPriority priority = MailPriority.Normal)
        {
            var message = new MailMessage();
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            message.Priority = priority;
            message.Subject = subject;
            message.Sender = new MailAddress(OperationsAPI.SenderMail);
            message.From = message.Sender;

            var recepEnum = mailRecepients.GetEnumerator();

            while (recepEnum.MoveNext())
            {
                message.To.Add(recepEnum.Current);
            }

            var copyRecepEnum = mailCopyRecepients.GetEnumerator();

            while (copyRecepEnum.MoveNext())
            {
                message.CC.Add(copyRecepEnum.Current);
            }

            message.Body = this.MessageBody;

            mailClient.Send(message);
        }

        public void Dispose()
        {
            mailClient.Dispose();
        }
    }
}
