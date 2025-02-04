﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommerceApiSDK.Models;
using CommerceApiSDK.Models.Enums;
using CommerceApiSDK.Models.Parameters;
using CommerceApiSDK.Models.Results;
using CommerceApiSDK.Services.Interfaces;
using CommerceApiSDK.Services.Messages;
using Newtonsoft.Json;

namespace CommerceApiSDK.Services
{
    public class CartService : ServiceBase, ICartService
    {
        private readonly IMessengerService optiMessenger;

        private OptiSubscriptionToken subscriptionToken;

        private List<AddCartLine> addToCartRequests = new List<AddCartLine>();

        public event EventHandler OnIsAddingToCartSlowChange;
        public event EventHandler OnAddToCartRequestsCountChange;

        private bool isAddingToCartSlow = false;
        public bool IsAddingToCartSlow => isAddingToCartSlow;

        public int AddToCartRequestsCount => addToCartRequests.Count;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public CartService(
            IClientService ClientService,
            INetworkService NetworkService,
            ITrackingService TrackingService,
            ICacheService CacheService,
            ILoggerService LoggerService,
            IMessengerService optiMessenger
        ) : base(ClientService, NetworkService, TrackingService, CacheService, LoggerService)
        {
            this.optiMessenger = optiMessenger;

            isCartEmpty = true;
            subscriptionToken = this.optiMessenger.Subscribe<UserSignedOutOptiMessage>(UserSignedOutHandler);
        }

        public event PropertyChangedEventHandler IsCartEmptyPropertyChanged;
        private bool isCartEmpty;
        public bool IsCartEmpty
        {
            get => isCartEmpty;
            set
            {
                if (isCartEmpty != value)
                {
                    isCartEmpty = value;
                    IsCartEmptyPropertyChanged?.Invoke(
                        this,
                        new PropertyChangedEventArgs("IsCartEmpty")
                    ); // Raise the event
                }
            }
        }

        public async Task<Cart> GetCart(Guid cartId, CartQueryParameters parameters)
        {
            try
            {
                string url = $"{CommerceAPIConstants.CartsUrl}/{cartId}";
                url += parameters.ToQueryString();

                Cart result = await GetAsyncNoCache<Cart>(url);

                IsCartEmpty = result?.CartLines == null || result.CartLines.Count <= 0;

                return result;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }

        private async Task<Cart> GetCart(CartQueryParameters parameters, AddCartModel addCartModel, CartType cartType = CartType.Regular)
        {
            try
            {
                string url = CommerceAPIConstants.CartCurrentUrl;

                Cart result = null;
                if (cartType == CartType.Alternate)
                {
                    url = CommerceAPIConstants.CartsUrl;
                    StringContent stringContent = await Task.Run(() => SerializeModel(addCartModel));
                    result = await PostAsyncNoCache<Cart>(url, stringContent);
                }
                else
                {                    
                    if (cartType == CartType.Regular)
                    {
                        await this.ClientService.RemoveAlternateCartCookie();
                    }
                    url += parameters.ToQueryString();
                    result = await GetAsyncNoCache<Cart>(url);
                }       

                IsCartEmpty = result?.CartLines == null || result.CartLines.Count <= 0;

                return result;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }

        public async Task<Cart> CreateAlternateCart(AddCartModel addCartModel)
        {
            return await GetCart(null, addCartModel, CartType.Alternate);
        }

        public async Task<Cart> GetCurrentCart(CartQueryParameters parameters)
        {
            return await GetCart(parameters, null, CartType.Current);
        }

        public async Task<Cart> GetRegularCart(CartQueryParameters parameters)
        {
            return await GetCart(parameters, null, CartType.Regular);
        }

        public async Task<GetCartLinesResult> GetCartLines()
        {
            try
            {
                List<string> parameters = new List<string>();

                string url = CommerceAPIConstants.CartCurrentCartLinesUrl;
                GetCartLinesResult result = await GetAsyncNoCache<GetCartLinesResult>(url);

                IsCartEmpty = result?.CartLines == null || result.CartLines.Count <= 0;

                return result;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }

        public async Task<PromotionCollectionModel> GetCurrentCartPromotions()
        {
            try
            {
                string url = CommerceAPIConstants.CartCurrentPromotionsUrl;
                PromotionCollectionModel result = await GetAsyncNoCache<PromotionCollectionModel>(
                    url
                );

                return result;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }

        public async Task<PromotionCollectionModel> GetCartPromotions(Guid cartId)
        {
            try
            {
                string url = string.Format(CommerceAPIConstants.CartPromotionsUrl, cartId);
                PromotionCollectionModel result = await GetAsyncNoCache<PromotionCollectionModel>(
                    url
                );

                return result;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }

        public async Task<ServiceResponse<Promotion>> ApplyPromotion(AddPromotion promotion)
        {
            try
            {
                string url = CommerceAPIConstants.CartCurrentPromotionsUrl;
                StringContent stringContent = await Task.Run(() => SerializeModel(promotion));
                ServiceResponse<Promotion> result = await PostAsyncNoCacheWithErrorMessage<Promotion>(url, stringContent);
                return result;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }

        public async Task<ServiceResponse<Cart>> UpdateCart(Cart cart)
        {
            try
            {
                string url = CommerceAPIConstants.CartCurrentUrl;
                StringContent stringContent = await Task.Run(() => SerializeModel(cart));
                ServiceResponse<Cart> result = await PatchAsyncNoCacheWithErrorMessage<Cart>(url, stringContent);

                return result;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }

        public async Task<bool> ClearCart()
        {
            try
            {
                HttpResponseMessage clearCartResponse = await DeleteAsync(
                    CommerceAPIConstants.CartCurrentUrl
                );
                bool result = clearCartResponse != null && clearCartResponse.IsSuccessStatusCode;

                if (result)
                {
                    IsCartEmpty = true;
                }

                return result;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return false;
            }
        }

        private void UserSignedOutHandler(OptiMessage message)
        {
            IsCartEmpty = true;
        }

        public async Task<ServiceResponse<CartLineCollectionDto>> AddWishListToCart(Guid wishListId)
        {
            try
            {
                return await PostAsyncNoCacheWithErrorMessage<CartLineCollectionDto>(
                    "api/v1/carts/current/cartlines/wishlist/" + wishListId,
                    null
                );
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }

        public async Task<CartCollectionModel> GetCarts(CartsQueryParameters parameters = null)
        {
            try
            {
                string url = CommerceAPIConstants.CartsUrl;

                if (parameters != null)
                {
                    string queryString = parameters.ToQueryString();
                    url += queryString;
                }

                return await GetAsyncNoCache<CartCollectionModel>(url);
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }

        public async Task<bool> DeleteCart(Guid cartId)
        {
            if (cartId.Equals(Guid.Empty))
            {
                throw new ArgumentException($"{nameof(cartId)} is empty");
            }

            try
            {
                string url = $"{CommerceAPIConstants.CartsUrl}/{cartId}";
                HttpResponseMessage deleteCartResponse = await DeleteAsync(url);
                return deleteCartResponse != null && deleteCartResponse.IsSuccessStatusCode;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return false;
            }
        }

        public async Task<ServiceResponse<CartLine>> AddCartLine(AddCartLine cartLine)
        {
            ServiceResponse<CartLine> result = null;
            try
            {
                addToCartRequests.Add(cartLine);
                OnAddToCartRequestsCountChange?.Invoke(this, null);
                MarkCurrentlyAddingCartLinesFlagToTrueIfNeeded();

                StringContent stringContent = await Task.Run(() => SerializeModel(cartLine));
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                result = await PostAsyncNoCacheWithErrorMessage<CartLine>(
                    CommerceAPIConstants.CartCurrentCartLineUrl,
                    stringContent, null);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                this.TrackingService.TrackException(exception);
            }
            finally
            {
                addToCartRequests.Remove(cartLine);
                OnAddToCartRequestsCountChange?.Invoke(this, null);
                MarkCurrentlyAddingCartLinesFlagTоFalseIfPossible();
            }

            return result;
        }

        [Obsolete("Caution: Will be removed in a future release.")]
        public void CancelAllAddCartLineTasks()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
        }

        [Obsolete("Caution: Will be removed in a future release.")]
        private async void MarkCurrentlyAddingCartLinesFlagToTrueIfNeeded()
        {
            await Task.Delay(CommerceAPIConstants.AddingToCartMillisecondsDelay);

            if (addToCartRequests.Count > 0)
            {
                isAddingToCartSlow = true;
                OnIsAddingToCartSlowChange?.Invoke(this, null);
            }
        }

        [Obsolete("Caution: Will be removed in a future release.")]
        private void MarkCurrentlyAddingCartLinesFlagTоFalseIfPossible()
        {
            if (addToCartRequests.Count == 0)
            {
                isAddingToCartSlow = false;
                OnIsAddingToCartSlowChange?.Invoke(this, null);
            }
        }

        public async Task<ServiceResponse<CartLine>> UpdateCartLine(CartLine cartLine)
        {
            try
            {
                StringContent stringContent = await Task.Run(() => SerializeModel(cartLine));
                return await PatchAsyncNoCacheWithErrorMessage<CartLine>(
                    $"{CommerceAPIConstants.CartCurrentCartLineUrl}/{cartLine.Id}",
                    stringContent
                );
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }

        public async Task<bool> DeleteCartLine(CartLine cartLine)
        {
            try
            {
                HttpResponseMessage result = await DeleteAsync(
                    $"{CommerceAPIConstants.CartCurrentCartLineUrl}/{cartLine.Id}"
                );
                return result.IsSuccessStatusCode;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return false;
            }
        }

        public async Task<ServiceResponse<CartLineList>> AddCartLineCollection(
            List<AddCartLine> cartLineCollection
        )
        {
            try
            {
                JsonSerializerSettings serializationSettings = new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                StringContent stringContent = await Task.Run(
                    () =>
                        SerializeModel(
                            new { cartLines = cartLineCollection },
                            serializationSettings
                        )
                );
                ServiceResponse<CartLineList> result = await PostAsyncNoCacheWithErrorMessage<CartLineList>(
                    CommerceAPIConstants.CartCurrentCartLineUrl + "/batch",
                    stringContent
                );
                return result;
            }
            catch (Exception exception)
            {
                this.TrackingService.TrackException(exception);
                return null;
            }
        }
    }
}
