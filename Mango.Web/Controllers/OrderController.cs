using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;

namespace Mango.Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [Authorize]
        [HttpGet]
        public IActionResult OrderIndex()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int orderId)
        {
            string userId = User.Claims.Where(x => x.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
            OrderHeaderDto orderHeaderDto = new OrderHeaderDto();

            var response = await _orderService.GetOrder(orderId);

            if (response is not null && response.IsSuccess)
            {
                orderHeaderDto = JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(response.Result));
            }

            if(!User.IsInRole(SD.RoleAdmin) && userId != orderHeaderDto.UserId)
            {
                return NotFound();
            }
            return View(orderHeaderDto);
        }

        [HttpPost("OrderReadyForPickup")]
        public async Task<IActionResult> OrderReadyForPickup(int orderId)
        {
            var response = await _orderService.UpdateOrderStatus(orderId, SD.Status_ReadyForPickup);

            if (response is not null && response.IsSuccess)
            {
                TempData["success"] = "Status updated successfully.";
                return RedirectToAction(nameof(OrderDetails), new { orderId = orderId });
            }
            return View();
        }

        [HttpPost("CompleteOrder")]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var response = await _orderService.UpdateOrderStatus(orderId, SD.Status_Complete);

            if (response is not null && response.IsSuccess)
            {
                TempData["success"] = "Status updated successfully.";
                return RedirectToAction(nameof(OrderDetails), new { orderId = orderId });
            }
            return View();
        }        
        
        [HttpPost("CancelOrder")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var response = await _orderService.UpdateOrderStatus(orderId, SD.Status_Cancelled);

            if (response is not null && response.IsSuccess)
            {
                TempData["success"] = "Status updated successfully.";
                return RedirectToAction(nameof(OrderDetails), new { orderId = orderId });
            }
            return View();
        }

        [HttpGet]
        public IActionResult GetAll([FromQuery] string status)
        {
            IEnumerable<OrderHeaderDto> list;
            string userId = string.Empty;
            if(!User.IsInRole(SD.RoleAdmin))
            {
                userId = User.Claims.Where(x => x.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
            }

            ResponseDto response = _orderService.GetAllOrders(userId).GetAwaiter().GetResult();
            if(response is not null && response.IsSuccess)
            {
                list = JsonConvert.DeserializeObject<List<OrderHeaderDto>>(Convert.ToString(response.Result));

                switch(status)
                {
                    case "approved":
                        list = list.Where(x=>x.Status == SD.Status_Approved);
                        break;
                    case "readyforpickup":
                        list = list.Where(x => x.Status == SD.Status_ReadyForPickup);
                        break;
                    case "cancelled":
                        list = list.Where(x => x.Status == SD.Status_Cancelled || x.Status == SD.Status_Refunded);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                list= new List<OrderHeaderDto>();
            }
            return Json(new { data = list});
        }
    }
}
