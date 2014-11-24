/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Interfaces.Module
{
    public interface IEntityNameProvider
    {
        string Read(ILightDomainModel domainModel, int collectionId, long entityId);
        string GetEntityTypeName(int collectionId);
        string EntityLink(UrlHelper urlHelper, int collectionId, long entityId, ILightDomainModel domainModel);
    }
}
