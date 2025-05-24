using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Volunteer_website.ViewModels;
using Volunteer_website.Models;
using Volunteer_website.VnPay;
using Libraries;
namespace Volunteer_website.Controllers
{
    public class VNPayController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly VolunteerManagementContext _context;

        public VNPayController(IConfiguration configuration, VolunteerManagementContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("VNPay/CreatePaymentUrl")]
        public IActionResult CreatePaymentUrl(DonationModel model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            // Lấy thông tin người dùng từ DB nếu có
            var volunteer = _context.Volunteers.FirstOrDefault(v => v.VolunteerId == userId);

            // Build VNPAY payment request
            var tick = DateTime.Now.Ticks.ToString();
            var vnpay = new VnPayLibrary();
            var url = _configuration["Vnpay:BaseUrl"];
            var returnUrl = $"{Request.Scheme}://{Request.Host}/Vnpay/PaymentReturn";
            var tmnCode = _configuration["Vnpay:TmnCode"];
            var hashSecret = _configuration["Vnpay:HashSecret"];

            if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(returnUrl) || string.IsNullOrEmpty(hashSecret))
            {
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
            vnpay.AddRequestData("vnp_TxnRef", tick); // dùng làm mã giao dịch

            var paymentUrl = vnpay.CreateRequestUrl(url, hashSecret);
            if (string.IsNullOrEmpty(paymentUrl))
            {
                TempData["Error"] = "Không thể tạo URL thanh toán.";
                return RedirectToAction("Index", "Home");
            }

            TempData["DonationTempData"] = JsonConvert.SerializeObject(new
            {
                EventId = model.Event_id,
                VolunteerId = userId,
                Amount = model.Amount,
                Note = model.Note
            });

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
                    vnpayData.AddResponseData(key, value);  
                }
            }

            var tmnCode = _configuration["VNPay:TmnCode"];
            var hashSecret = _configuration["VNPay:HashSecret"];
            var vnp_SecureHash = Request.Query["vnp_SecureHash"];

            if (string.IsNullOrEmpty(vnp_SecureHash) || string.IsNullOrEmpty(hashSecret))
            {
                TempData["Error"] = "Dữ liệu thanh toán không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            var checkSignature = vnpayData.ValidateSignature(vnp_SecureHash, hashSecret);

            if (checkSignature)
            {
                var responseCode = vnpayData.GetResponseData("vnp_ResponseCode");
                if (responseCode == "00")
                {
                    // Thanh toán thành công → lưu DB
                    if (TempData["DonationTempData"] is string donationTempDataJson && !string.IsNullOrEmpty(donationTempDataJson))
                    {
                        var donationData = JsonConvert.DeserializeObject<dynamic>(donationTempDataJson);
                        string gen_id = Guid.NewGuid().ToString();

                        var donation = new Models.Donation
                        {
                            DonationId = gen_id,
                            EventId = donationData.EventId,
                            VolunteerId = donationData.VolunteerId,
                            Amount = donationData.Amount,
                            Message = donationData.Note,
                            DonationDate = DateTime.Now,
                        };

                        _context.Donations.Add(donation);
                        await _context.SaveChangesAsync();

                        TempData["Message"] = "Ủng hộ thành công! Cảm ơn bạn!";
                        return RedirectToAction("Detail_Event", "Home", new { id = donationData.EventId.ToString() });
                    }
                }
            }

            TempData["Error"] = "Thanh toán thất bại hoặc bị hủy.";
            return RedirectToAction("Events", "Home");
        }
    }

}
