using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Volunteer_website.ViewModels;
using Volunteer_website.Models;
using Volunteer_website.VnPay;
using Libraries;
using System.Security.Claims;
using Volunteer_website.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Volunteer_website.Controllers
{
    public class VNPayController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly VolunteerManagementContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<VNPayController> _logger;

        public VNPayController(IConfiguration configuration, VolunteerManagementContext context, EmailService emailService, ILogger<VNPayController> logger)
        {
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("VNPay/CreatePaymentUrl")]
        public IActionResult CreatePaymentUrl(DonationModel model)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? role = User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation($"CreatePaymentUrl: userId={userId}, EventId={model.Event_id}, Amount={model.Amount}");

            // Lấy thông tin người dùng từ DB nếu có
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UserId is null or empty");
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Login", "Account");
            }
            var volunteer = _context.Volunteers.FirstOrDefault(v => v.VolunteerId == userId);
            if (volunteer == null)
            {
                _logger.LogWarning($"Volunteer not found for userId={userId}");
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Login", "Account");
            }

            // Build VNPAY payment request
            var tick = DateTime.Now.Ticks.ToString();
            var vnpay = new VnPayLibrary();
            var url = _configuration["Vnpay:BaseUrl"];
            var returnUrl = $"{Request.Scheme}://{Request.Host}/Vnpay/PaymentReturn";
            var tmnCode = _configuration["Vnpay:TmnCode"];
            var hashSecret = _configuration["Vnpay:HashSecret"];

            if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(returnUrl) || string.IsNullOrEmpty(hashSecret))
            {
                _logger.LogError("Invalid VnPay configuration");
                TempData["Error"] = "Cấu hình thanh toán không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", tmnCode);
            vnpay.AddRequestData("vnp_Amount", ((int)model.Amount * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", model.Note ?? "Ủng hộ sự kiện");
            vnpay.AddRequestData("vnp_OrderType", "donation");
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnpay.AddRequestData("vnp_TxnRef", tick);

            var paymentUrl = vnpay.CreateRequestUrl(url, hashSecret);
            if (string.IsNullOrEmpty(paymentUrl))
            {
                _logger.LogError("Failed to create VnPay payment URL");
                TempData["Error"] = "Không thể tạo URL thanh toán.";
                return RedirectToAction("Index", "Home");
            }

            TempData["DonationTempData"] = JsonConvert.SerializeObject(new
            {
                EventId = model.Event_id,
                VolunteerId = userId,
                Amount = model.Amount,
                Note = model.Note,
                TxnRef = tick
            });

            _logger.LogInformation($"Created payment URL: {paymentUrl}");
            return Redirect(paymentUrl);
        }

        public async Task<IActionResult> PaymentReturn()
        {
            var vnpayData = new VnPayLibrary();
            var responseData = Request.Query;

            foreach (var (key, value) in responseData)
            {
                if (key.StartsWith("vnp_") && !string.IsNullOrEmpty(value))
                {
                    vnpayData.AddResponseData(key, value!);
                }
            }

            var tmnCode = _configuration["VNPay:TmnCode"];
            var hashSecret = _configuration["VNPay:HashSecret"];
            var vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString();
            var vnp_TxnRef = vnpayData.GetResponseData("vnp_TxnRef");

            _logger.LogInformation($"PaymentReturn: vnp_TxnRef={vnp_TxnRef}, vnp_SecureHash={vnp_SecureHash}");

            if (string.IsNullOrEmpty(vnp_SecureHash) || string.IsNullOrEmpty(hashSecret))
            {
                _logger.LogError("Invalid payment data: Missing vnp_SecureHash or hashSecret");
                TempData["Error"] = "Dữ liệu thanh toán không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            var checkSignature = vnpayData.ValidateSignature(vnp_SecureHash, hashSecret);

            if (checkSignature)
            {
                var responseCode = vnpayData.GetResponseData("vnp_ResponseCode");
                _logger.LogInformation($"PaymentReturn: ResponseCode={responseCode}");

                if (responseCode == "00")
                {
                    // Thanh toán thành công → lưu DB
                    if (TempData["DonationTempData"] is string donationTempDataJson && !string.IsNullOrEmpty(donationTempDataJson))
                    {
                        _logger.LogInformation($"PaymentReturn: Deserializing DonationTempData={donationTempDataJson}");
                        var donationData = JsonConvert.DeserializeObject<Dictionary<string, object>>(donationTempDataJson);

                        if (donationData == null ||
                            !donationData.TryGetValue("EventId", out var eventIdObj) ||
                            !donationData.TryGetValue("VolunteerId", out var volunteerIdObj) ||
                            !donationData.TryGetValue("Amount", out var amountObj))
                        {
                            _logger.LogWarning("PaymentReturn: DonationTempData missing required fields");
                            TempData["Error"] = "Không tìm thấy thông tin donation.";
                            return RedirectToAction("Index", "Home");
                        }

                        string eventId = eventIdObj?.ToString() ?? string.Empty;
                        string volunteerId = volunteerIdObj?.ToString() ?? string.Empty;
                        decimal amount = 0;
                        if (amountObj is decimal dec)
                            amount = dec;
                        else if (decimal.TryParse(amountObj?.ToString(), out var parsedAmount))
                            amount = parsedAmount;

                        string? note = donationData.TryGetValue("Note", out var noteObj) ? noteObj?.ToString() : null;


                        string newId = "DON0001";
                        var maxId = await _context.Donations
                            .Select(e => e.DonationId)
                            .OrderByDescending(id => id)
                            .FirstOrDefaultAsync();
                        if (!string.IsNullOrEmpty(maxId) && maxId.StartsWith("DON")) // Sửa "D0N" thành "DON"
                        {
                            if (int.TryParse(maxId.Substring(3), out int numericPart))
                            {
                                newId = $"DON{(numericPart + 1):D4}";
                            }
                        }


                        var donation = new Models.Donation
                        {
                            DonationId = newId,
                            EventId = eventId,
                            VolunteerId = volunteerId,
                            Amount = amount,
                            Message = note,
                            DonationDate = DateTime.Now,
                        };

                        try
                        {
                            _context.Donations.Add(donation);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Donation saved: DonationId={newId}, VolunteerId={volunteerId}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to save donation for vnp_TxnRef={newId}");
                            TempData["Error"] = "Thanh toán thành công nhưng không thể lưu donation.";
                            return RedirectToAction("Index", "Home");
                        }

                        // Gửi email xác nhận
                        try
                        {
                            var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.VolunteerId == volunteerId);
                            var eventModel = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
                            if (volunteer != null && !string.IsNullOrEmpty(volunteer.Email))
                            {
                                string subject = "Cảm ơn bạn đã ủng hộ sự kiện!";
                                string body = $@"
                                            <h3>Xin chào {volunteer.Name},</h3>
                                            <p>Cảm ơn bạn đã ủng hộ sự kiện <strong>{eventModel?.Name}</strong>!</p>
                                            <p><strong>Số tiền ủng hộ:</strong> {string.Format("{0:N0} VND", donation.Amount)}</p>
                                            <p><strong>Thời gian ủng hộ:</strong> {donation.DonationDate?.ToString("HH:mm - dd/MM/yyyy")}</p>
                                            <p><strong>Lời nhắn:</strong> {donation.Message ?? "Không có lời nhắn"}</p>
                                            <p>Chúng tôi rất trân trọng sự đóng góp của bạn để giúp sự kiện thành công!</p>
                                            <p>Trân trọng,<br>Đội ngũ Volunteer Website</p>";

                                await _emailService.SendEmailAsync(volunteer.Email, subject, body);
                                _logger.LogInformation($"Email sent successfully to {volunteer.Email}");
                            }
                            else
                            {
                                _logger.LogWarning($"No valid volunteer or email for VolunteerId={volunteerId}");
                                TempData["Error"] = "Thanh toán thành công nhưng không tìm thấy thông tin người ủng hộ.";
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send email for VolunteerId={volunteerId}");
                            TempData["Error"] = "Thanh toán thành công nhưng không gửi được email xác nhận.";
                        }

                        TempData["Message"] = "Ủng hộ thành công! Cảm ơn bạn!";
                        return RedirectToAction("Detail_Event", "Home", new { id = eventId });
                    }
                    else
                    {
                        _logger.LogWarning("PaymentReturn: DonationTempData is null or empty");
                        TempData["Error"] = "Không tìm thấy thông tin donation.";
                        return RedirectToAction("Index", "Home");
                    }
                }
            }

            _logger.LogWarning($"Payment failed or invalid signature for vnp_TxnRef={vnp_TxnRef}");
            TempData["Error"] = "Thanh toán thất bại hoặc bị hủy.";
            return RedirectToAction("Events", "Home");
        }
    }
}