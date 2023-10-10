using AutoMapper;
using Mango.MessageBus;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.RabbitMQSender;
using Mango.Services.OrderAPI.Service.IService;
using Mango.Services.OrderAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Mango.Services.OrderAPI.Controllers
{
    [Route("api/Order")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        protected ResponseDto _response;
        private IMapper _mapper;
        private readonly AppDbContext _db;
        private IProductService _productService;
        private readonly IRabbitMQOrderMessageSender _messageBus;
        private readonly IConfiguration _configuration;

        public OrderAPIController(AppDbContext db,
            IProductService productService,
            IMapper mapper,
            IConfiguration configuration,
            IRabbitMQOrderMessageSender messageBus)
        {
            _db = db;
            _mapper = mapper;
            _productService = productService;
            _response = new ResponseDto();
            _configuration = configuration;
            _messageBus = messageBus;

        }

        [Authorize]
        [HttpPost("CreateOrder")]
        public async Task<ResponseDto> CreateOrder([FromBody] CartDto cartDto)
        {
            try
            {
                var orderHeaderDto = _mapper.Map<OrderHeaderDto>(cartDto.CartHeader);
                orderHeaderDto.OrderTime = DateTime.Now;
                orderHeaderDto.Status = SD.Status_Pending;
                orderHeaderDto.OrderDetails = _mapper.Map<IEnumerable<OrderDetailsDto>>(cartDto.CartDetails);

                var orderCreated = _db.OrderHeaders.Add(_mapper.Map<OrderHeader>(orderHeaderDto)).Entity;
                await _db.SaveChangesAsync();

                orderHeaderDto.OrderHeaderId = orderCreated.OrderHeaderId;
                _response.Result = orderHeaderDto;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPost("CreateStripeSession")]
        public async Task<ResponseDto> CreateStripeSession([FromBody] StripeRequestDto stripeRequestDto)
        {
            try
            {

                var options = new SessionCreateOptions
                {
                    SuccessUrl = stripeRequestDto.ApprovedUrl,
                    CancelUrl = stripeRequestDto.CancelUrl,
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",

                };

                var discountObj = new List<SessionDiscountOptions>()
                {
                    new SessionDiscountOptions
                    {
                        Coupon = stripeRequestDto.OrderHeader.CouponCode
                    }
                };

                foreach (var item in stripeRequestDto.OrderHeader.OrderDetails)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions()
                        {
                            UnitAmount = (long)(item.Product.Price * 100),// $20.99 - 2099
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Name
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                if (stripeRequestDto.OrderHeader.Discount > 0)
                {
                    options.Discounts = discountObj;
                }

                var service = new SessionService();
                var session = service.Create(options);
                stripeRequestDto.StripeSessionUrl = session.Url;

                var orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == stripeRequestDto.OrderHeader.OrderHeaderId);
                orderHeader.StripeSessionId = session.Id;
                _db.SaveChanges();
                _response.Result = stripeRequestDto;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPost("ValidateStripeSession")]
        public async Task<ResponseDto> ValidateStripeSession([FromBody] int orderHeaderId)
        {
            try
            {
                var orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == orderHeaderId);

                var service = new SessionService();
                var session = service.Get(orderHeader.StripeSessionId);
                
                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = paymentIntentService.Get(session.PaymentIntentId);

                if (paymentIntent.Status == "succeeded")
                { 
                    orderHeader.PaymentIntentId = paymentIntent.Id;
                    orderHeader.Status = SD.Status_Approved;
                    _db.SaveChanges();

                    var rewardDto = new RewardsDto()
                    {
                        OrderId = orderHeader.OrderHeaderId,
                        RewardsActivity = Convert.ToInt32(orderHeader.OrderTotal),
                        UserId = orderHeader.UserId
                    };

                    string topicName = _configuration.GetValue<string>("TopicAndQueueName:OrderCreatedTopics");
                    _messageBus.SendMessage(rewardDto, topicName);
                    _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
                }

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpGet("GetOrder/{id:int}")]
        public ResponseDto? Get(int id)
        {
            try
            {
                var orderHeader = _db.OrderHeaders.Include(u => u.OrderDetails).First(u => u.OrderHeaderId == id);
                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
            }
            catch(Exception ex) 
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpGet("GetOrders")]
        public ResponseDto? Get(string? userId = "")
        {
            try
            {
                IEnumerable<OrderHeader> objList;
                if(User.IsInRole(SD.RoleAdmin))
                {
                    objList = _db.OrderHeaders.Include(u => u.OrderDetails).OrderByDescending(x => x.OrderHeaderId).ToList();
                }
                else
                {
                    objList = _db.OrderHeaders.Include(u => u.OrderDetails).Where(x => x.UserId == userId).OrderByDescending(x => x.OrderHeaderId).ToList();
                }
                _response.Result = _mapper.Map<IEnumerable<OrderHeaderDto>>(objList);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPost("UpdateOrderStatus/{orderId:int}")]
        public async Task<ResponseDto> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            try
            {
                var orderHeader = _db.OrderHeaders.First(x => x.OrderHeaderId == orderId);
                if(orderHeader is not null)
                {
                    if(newStatus == SD.Status_Cancelled)
                    {
                        var options = new RefundCreateOptions
                        {
                            Reason = RefundReasons.RequestedByCustomer,
                            PaymentIntent = orderHeader.PaymentIntentId
                        };

                        var service = new RefundService();
                        var refund = service.Create(options);
                    }
                    orderHeader.Status = newStatus;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess=false;
                _response.Message = ex.Message;
            }
            return _response;
        }

    }
}
