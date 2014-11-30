/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Engine.Currencies
{
    public class CurrencyCache : AbstractCacheHandler
    {
        private const string CookieCurrencyName = "currency";
        private readonly Dictionary<long, CurrencyCacheItem> _currencies = new Dictionary<long, CurrencyCacheItem>();
        private readonly List<CurrencyCacheItem> _currencyList = new List<CurrencyCacheItem>(); 
        private readonly IDomainModelProvider _domainModelProvider;
        private long? _baseCurrencyId;

        public CurrencyCache(IDomainModelProvider domainModelProvider) : base(new[]{typeof(Currency)})
        {
            _domainModelProvider = domainModelProvider;
        }

        public IReadOnlyList<CurrencyCacheItem> Currencies { get { return _currencyList; } }

        public long? CurrentCurrencyId
        {
            get
            {
                var context = HttpContext.Current;

                var cookieCurrency = context.Request.Cookies[CookieCurrencyName];
                if (cookieCurrency != null)
                {
                    long currencyId;
                    if (long.TryParse(cookieCurrency.Value, out currencyId) && _currencies.ContainsKey(currencyId))
                    {
                        return currencyId;
                    }
                }

                return _baseCurrencyId;
            }
            set
            {
                if (!value.HasValue)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                var context = HttpContext.Current;
                context.Response.Cookies.Remove(CookieCurrencyName);
                context.Response.Cookies.Add(new HttpCookie(CookieCurrencyName, value.Value.ToString(CultureInfo.InvariantCulture)));
            }
        }

        public CurrencyCacheItem CurrentCurrency
        {
            get
            {
                var currencyId = CurrentCurrencyId;
                if (!currencyId.HasValue)
                {
                    return null;
                }
                CurrencyCacheItem currency;
                if (_currencies.TryGetValue(currencyId.Value, out currency))
                {
                    return currency;
                }
                return null;
            }
        }

        public double ToUserCurrency(double value)
        {
            var userCurrency = CurrentCurrency;
            if (userCurrency == null || !_baseCurrencyId.HasValue || userCurrency.CurrencyId == _baseCurrencyId.Value)
            {
                return value;
            }
            var shopCurrencyRate = _currencies[_baseCurrencyId.Value].Rate;
            var userCurrencyRate = userCurrency.Rate;

            if (Math.Abs(shopCurrencyRate - userCurrencyRate) < 0.01)
            {
                return value;
            }

            return (value / shopCurrencyRate) * userCurrencyRate;
        }

        protected override void Initialize(long? objectId)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var source in domainModel.Query().From<Currency>().Where("Enabled = $", true).ToList<Currency>())
                {
                    var currency = new CurrencyCacheItem(source.Id, source.Name, source.Code, source.Rate, source.IsBase);
                    _currencies.Add(currency.CurrencyId, currency);
                    _currencyList.Add(currency);

                    if (currency.IsBase)
                    {
                        _baseCurrencyId = currency.CurrencyId;
                    }
                }
            }
        }
    }
}
