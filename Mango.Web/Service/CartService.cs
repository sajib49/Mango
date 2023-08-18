using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;

namespace Mango.Web.Service
{
    public class CartService : ICartService
    {
        private readonly IBaseService _baseService;
        public CartService(IBaseService baseService)
        {
            _baseService = baseService;
        }

        public async Task<ResponseDto> ApplyCouponAsync(CartDto cartDto)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = SD.ApiType.POST,
                Url = SD.ShoppingCartAPIBase + "/api/cart/ApplyCoupon",
                Data = cartDto
            });
        }

        public async Task<ResponseDto> GetCartByUserId(string userId)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = SD.ApiType.GET,
                Url = SD.ShoppingCartAPIBase + "/api/cart/GetCart/"+userId,
            });
        }

        public async Task<ResponseDto> RemoveFromCartAsync(int cartDetailsId)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = SD.ApiType.POST,
                Url = SD.ShoppingCartAPIBase + "/api/cart/RemoveCart",
                Data = cartDetailsId
            });
        }

        public async Task<ResponseDto> UpsertCartAsync(CartDto cartDto)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = SD.ApiType.POST,
                Url = SD.ShoppingCartAPIBase + "/api/cart/CartUpsert",
                Data = cartDto
            });
        }


        //public async Task<ResponseDto> CreateCouponAsync(CouponDto couponDto)
        //{
        //    return await _baseService.SendAsync(new RequestDto
        //    {
        //        ApiType = SD.ApiType.POST,
        //        Url = SD.CouponAPIBase + "/api/coupon",
        //        Data = couponDto
        //    });
        //}

        //public async Task<ResponseDto> DeleteCouponAsync(int id)
        //{
        //    return await _baseService.SendAsync(new RequestDto
        //    {
        //        ApiType = SD.ApiType.DELETE,
        //        Url = SD.CouponAPIBase + "/api/coupon/" + id,
        //    });
        //}

        //public async Task<ResponseDto?> GetAllCouponsAsync()
        //{
        //    return await _baseService.SendAsync(new RequestDto
        //    {
        //        ApiType = SD.ApiType.GET,
        //        Url = SD.CouponAPIBase+ "/api/coupon",
        //    });
        //}

        //public async Task<ResponseDto> GetCouponAsync(string couponCode)
        //{
        //    return await _baseService.SendAsync(new RequestDto
        //    {
        //        ApiType = SD.ApiType.GET,
        //        Url = SD.CouponAPIBase + "/api/coupon/GetByCode/"+ couponCode,
        //    });
        //}

        //public async Task<ResponseDto> GetCouponByIdAsync(int id)
        //{
        //    return await _baseService.SendAsync(new RequestDto
        //    {
        //        ApiType = SD.ApiType.GET,
        //        Url = SD.CouponAPIBase + "/api/coupon/"+id,
        //    });
        //}

        //public async Task<ResponseDto> UpdateCouponAsync(CouponDto couponDto)
        //{
        //    return await _baseService.SendAsync(new RequestDto
        //    {
        //        ApiType = SD.ApiType.PUT,
        //        Url = SD.CouponAPIBase + "/api/coupon",
        //        Data = couponDto
        //    });
        //}
    }
}
