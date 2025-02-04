﻿using System.Threading.Tasks;
using CommerceApiSDK.Models.Parameters;
using CommerceApiSDK.Models.Results;

namespace CommerceApiSDK.Services.Interfaces
{
    public interface IWarehouseService
    {
        Task<GetWarehouseCollectionResult> GetWarehouses(WarehousesQueryParameters parameters);
    }
}
