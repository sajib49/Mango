﻿using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;

namespace Mango.Web.Service
{
    public class ProductService : IProductService
    {
        private readonly IBaseService _baseService;
        public ProductService(IBaseService baseService)
        {
            _baseService = baseService;
        }
        public async Task<ResponseDto> CreateProductAsync(ProductDto ProductDto)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = SD.ApiType.POST,
                Url = SD.ProductAPIBase + "/api/Product",
                Data = ProductDto, 
                ContentType = SD.ContentType.MultipartFormData
            });
        }

        public async Task<ResponseDto> DeleteProductAsync(int id)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = SD.ApiType.DELETE,
                Url = SD.ProductAPIBase + "/api/Product/" + id,
            });
        }

        public async Task<ResponseDto?> GetAllProductsAsync()
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = SD.ApiType.GET,
                Url = SD.ProductAPIBase+ "/api/Product",
            });
        }

        public async Task<ResponseDto> GetProductAsync(string ProductCode)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = SD.ApiType.GET,
                Url = SD.ProductAPIBase + "/api/Product/GetByCode/"+ ProductCode,
            });
        }

        public async Task<ResponseDto> GetProductByIdAsync(int id)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = SD.ApiType.GET,
                Url = SD.ProductAPIBase + "/api/Product/"+id,
            });
        }

        public async Task<ResponseDto> UpdateProductAsync(ProductDto ProductDto)
        {
            return await _baseService.SendAsync(new RequestDto
            {
                ApiType = SD.ApiType.PUT,
                Url = SD.ProductAPIBase + "/api/Product",
                Data = ProductDto,
                ContentType = SD.ContentType.MultipartFormData
            });
        }
    }
}
