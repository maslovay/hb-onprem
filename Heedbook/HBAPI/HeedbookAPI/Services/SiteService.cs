using System;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Models.Post;
using HBLib.Utils.Interfaces;

namespace UserOperations.Services
{
    public class SiteService
    {
        private readonly IMailSender _mailSender;

        public SiteService(
            IMailSender mailSender
            )
        {
            _mailSender = mailSender;
        }   
        public string Feedback([FromBody]FeedbackEntity feedback)
        {
            if(string.IsNullOrEmpty(feedback.name)
                || string.IsNullOrEmpty(feedback.phone)
                || string.IsNullOrEmpty(feedback.email)
                || string.IsNullOrEmpty(feedback.body))
                throw new Exception();

            string text = string.Format("<table>" +
                "<tr><td>name:</td><td> {0}</td></tr>" +
                "<tr><td>email:</td><td> {1}</td></tr>" +
                "<tr><td>phone:</td><td> {2}</td></tr>" +
                "<tr><td>message:</td><td> {3}</td></tr>" +
                "</table>", feedback.name, feedback.email, feedback.phone, feedback.body);
            _mailSender.SendSimpleEmail("info@heedbook.com", "Message from site", text);
            return "Sended";            
        }        
    }
}