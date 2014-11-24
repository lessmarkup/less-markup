/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Engine.TextAndSearch
{
    public interface ITextTagHandler
    {
        string GetTagHtml(string name, string argument, string contents, bool isClose, ILightDomainModel domainModel, UrlHelper urlHelper);
    }
}
