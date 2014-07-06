/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Engine.Currencies
{
    public class CurrencyCacheItem
    {
        private readonly long _currencyId;
        private readonly string _name;
        private readonly string _code;
        private readonly double _rate;
        private readonly bool _isBase;

        public CurrencyCacheItem(long currencyId, string name, string code, double rate, bool isBase)
        {
            _currencyId = currencyId;
            _name = name;
            _code = code;
            _rate = rate;
            _isBase = isBase;
        }

        public bool IsBase
        {
            get { return _isBase; }
        }

        public long CurrencyId
        {
            get { return _currencyId; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string Code
        {
            get { return _code; }
        }

        public double Rate
        {
            get { return _rate; }
        }
    }
}
