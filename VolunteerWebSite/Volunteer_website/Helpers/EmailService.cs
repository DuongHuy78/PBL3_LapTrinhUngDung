using System.Net.Mail;
using System.Net;

namespace Volunteer_website.Helpers
{
    public class EmailService
    {
        public static bool SendEmailMessage(string to, string subject, string body, string attachFile)
        {
            try
            {
                //Tạo đối tượng gửi mail
                MailMessage message = new MailMessage(ConstantHelper.emailSender, to, subject, body);
                // Tạo SMTP client kết nối đến máy chủ mail với:Tạo SMTP client kết nối đến máy chủ mail với:
                using (var client = new SmtpClient(ConstantHelper.hostEmail, ConstantHelper.port))
                {
                    client.EnableSsl = true;

                    if (!string.IsNullOrEmpty(attachFile))
                    {
                        Attachment attachment = new Attachment(attachFile);
                        message.Attachments.Add(attachment);
                    }


                    NetworkCredential credential = new NetworkCredential(ConstantHelper.emailSender, ConstantHelper.passwordSender);
                    client.UseDefaultCredentials = false;
                    client.Credentials = credential;
                    client.Send(message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e);
                return false;
            }
            return true;
        }
        public static bool SendNewPasswordEmail(string userEmail, string newPassword)
        {
            try
            {
                string subject = "Your New Password";
                string body = $@"
             Password Reset Successful
             Your password has been reset successfully.
             New Password: {newPassword}
             Please log in and change your password immediately for security reasons
            This is an automated email. Do not reply
        ";

                return SendEmailMessage(userEmail, subject, body, null);
            }
            catch
            {
                return false;
            }
        }

        public static bool SendAccountInfoEmail(string userEmail, string username, string password)
        {
            try
            {
                string subject = "Your Account Information";
                string body = $@"
        Account Information
        Your account has been created or updated successfully.
        Username: {username}
        Password: {password}
        Please log in and change your password immediately for security reasons.
        This is an automated email. Do not reply.
        ";

                return SendEmailMessage(userEmail, subject, body, null);
            }
            catch
            {
                return false;
            }
        }
    }
}
