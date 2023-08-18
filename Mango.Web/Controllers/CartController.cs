using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        public CartController(ICartService cartService) 
        {
            _cartService = cartService;
        }
        
        [Authorize]
        public async IActionResult CartIndex()
        {
            return View();
        }
    }
}
