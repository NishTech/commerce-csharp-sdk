﻿using CommerceApiSDK.Models;
using CommerceApiSDK.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace CommerceApiSDK.Services
{
    public class CatalogpagesService : ServiceBase, ICatalogpagesService
    {
        public CatalogpagesService(
            IClientService ClientService,
            INetworkService NetworkService,
            ITrackingService TrackingService,
            ICacheService CacheService,
            ILoggerService LoggerService
        ) : base(ClientService, NetworkService, TrackingService, CacheService, LoggerService) { }

        public async Task<CatalogPage> GetProductCatalogInformation(string productPath)
        {
            try
            {
                string url = $"{CommerceAPIConstants.CatalogPageUrl}{productPath}";

                CatalogPage productResult = await GetAsyncWithCachedResponse<CatalogPage>(url);

                if (productResult == null)
                {
                    return null;
                }

                return productResult;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }
    }
}
