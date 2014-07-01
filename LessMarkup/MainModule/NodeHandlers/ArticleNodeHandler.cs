/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Structure;
using LessMarkup.MainModule.Model;

namespace LessMarkup.MainModule.NodeHandlers
{
    public class ArticleNodeHandler : AbstractNodeHandler
    {
        public override object GetViewData(long objectId, object settings, object controller)
        {
            return new
            {
                ((ArticleModel)settings).Body
            };
        }

        public override Type SettingsModel
        {
            get { return typeof(ArticleModel); }
        }
    }
}
