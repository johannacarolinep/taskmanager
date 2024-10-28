using TaskManager.Models.Services;
using TaskManager.ViewModels;
using Microsoft.AspNetCore.Mvc;
namespace TaskManager.Controllers
{
    public class EmailController : Controller
    {
        private readonly EmailService _emailService;

        public EmailController(EmailService emailService)
        {
            _emailService = emailService;
        }
        [HttpGet]
        public IActionResult SendEmail()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SendEmail(EmailViewModel model) {
            if (ModelState.IsValid)
            {
                string subject = "You were invited to a new tasklist in TaskManager";
                string plainText = "You were invited to a new tasklist with name XXX.\n\nClick here to see your invitations: http://localhost:5206/ListUser/Invitations";
                string HtmlContent = @"
                    <h2>You've been invited to a new tasklist in TaskManager!</h2>
                    <p>You were invited to a new tasklist called <strong>XXX</strong>.</p>
                    <p>Click the link below to view your invitations:</p>
                    <p><a href='http://localhost:5206/ListUser/Invitations' style='color: #1E90FF; text-decoration: none;'>View Invitations</a></p>
                    <br />
                    <p>Best regards,<br />The TaskManager Team</p>";

                await _emailService.SendEmailAsync(model.ToEmail, subject, plainText, HtmlContent);
                ViewBag.Message = "Email sent successfully!";
            }
            return View(model);
        }
    }
}