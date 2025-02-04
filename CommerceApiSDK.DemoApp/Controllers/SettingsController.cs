﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommerceApiSDK.Models;
using CommerceApiSDK.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CommerceApiSDK.DemoApp.Controllers
{
    [ApiController]
    [Route("api/v1/settings")]
    public class SettingsController : Controller
    {
        private readonly ISettingsService settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        [HttpGet(Name = "Settings")]
        public async Task<Settings> Get()
        {
            return await this.settingsService.GetSettingsAsync();
        }

        [HttpGet]
        [Route("account")]
        [SwaggerOperation(
            Summary = "Returns account settings"
        )]
        public async Task<AccountSettings> GetAccountSettings()
        {
            return await this.settingsService.GetAccountSettingsAsync();
        }

        [HttpGet]
        [Route("cart")]
        [SwaggerOperation(
            Summary = "Returns cart settings"
        )]
        public async Task<CartSettings> GetCartSettings()
        {
            return await this.settingsService.GetCartSettingAsync();
        }

        [HttpGet]
        [Route("mobileapp")]
        [SwaggerOperation(
            Summary = "Returns mobile app settings"
        )]
        public async Task<MobileAppSettings> GetMobileAppSettings()
        {
            return await this.settingsService.GetMobileAppSettingAsync();
        }

        [HttpGet]
        [Route("products")]
        [SwaggerOperation(
            Summary = "Returns products settings"
        )]
        public async Task<ProductSettings> GetProductSettings()
        {
            return await this.settingsService.GetProductSettingsAsync();
        }

        [HttpGet]
        [Route("quote")]
        [SwaggerOperation(
            Summary = "Returns quote settings"
        )]
        public async Task<QuoteSettings> GetQuoteSettings()
        {
            return await this.settingsService.GetQuoteSettingAsync();
        }

        [HttpGet]
        [Route("website")]
        [SwaggerOperation(
            Summary = "Returns website settings"
        )]
        public async Task<WebsiteSettings> GetWebsiteSettings()
        {
            return await this.settingsService.GetWebsiteSettingsAsync();
        }

        [HttpGet]
        [Route("wishlist")]
        [SwaggerOperation(
            Summary = "Returns wishlist settings"
        )]
        public async Task<WishListSettings> GetWishlistSettings()
        {
            return await this.settingsService.GetWishListSettingAsync();
        }
    }
}

