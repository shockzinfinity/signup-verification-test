﻿using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using signup_verification.Helpers;

namespace signup_verification.Services
{
  public interface IEmailService
  {
    void Send(string to, string subject, string html, string from = null);
  }

  public class EmailService : IEmailService
  {
    private readonly AppSettings _appSettings;

    public EmailService(IOptions<AppSettings> appSettings)
    {
      _appSettings = appSettings.Value;
    }

    public void Send(string to, string subject, string html, string from = null)
    {
      var email = new MimeMessage();
      email.Sender = MailboxAddress.Parse(from ?? _appSettings.EmailFrom);
      email.To.Add(MailboxAddress.Parse(to));
      email.Subject = subject;
      email.Body = new TextPart(TextFormat.Html) { Text = html };

      using (var smtp = new SmtpClient())
      {
        smtp.Connect(_appSettings.SmtpHost, _appSettings.SmtpPort, SecureSocketOptions.StartTls);
        smtp.Authenticate(_appSettings.SmtpUser, _appSettings.SmtpPassword);
        smtp.Send(email);
        smtp.Disconnect(true);
      }
    }
  }
}