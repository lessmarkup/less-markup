/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Framework.Site
{
    public class SiteMapperScope : IDisposable
    {
        public SiteMapperScope()
        {
            if (SiteMapper.IsMappingSet())
            {
                throw new ArgumentException();
            }
        }

        public static void ResetMapping()
        {
            SiteMapper.ResetMapping();
        }

        public void Dispose()
        {
            SiteMapper.ResetMapping();
        }
    }
}
