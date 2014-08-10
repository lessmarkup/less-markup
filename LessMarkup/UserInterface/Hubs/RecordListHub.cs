/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Threading.Tasks;
using LessMarkup.UserInterface.ChangeTracking;

namespace LessMarkup.UserInterface.Hubs
{
    /*public class RecordListHub : Hub
    {
        public static RecordChangeTracker ChangeTracker { get; set; }

        public void RegisterForChanges(string modelId, string filter)
        {
            if (!OnBeginRequest())
            {
                return;
            }
            try
            {
                ChangeTracker.RegisterChannel(Context.ConnectionId, modelId, filter);
            }
            finally
            {
                OnEndRequest();
            }
        }

        public void GetAllIds()
        {
            if (!OnBeginRequest())
            {
                return;
            }
            try
            {
                ChangeTracker.GetAllIds(Context.ConnectionId);
            }
            finally
            {
                OnEndRequest();
            }
        }

        public void GetRecords(List<long> recordIds)
        {
            if (!OnBeginRequest())
            {
                return;
            }
            try
            {
                ChangeTracker.GetRecords(Context.ConnectionId, recordIds);
            }
            finally
            {
                OnEndRequest();
            }
        }

        public override Task OnDisconnected()
        {
            var channelId = Context.ConnectionId;
            ChangeTracker.DeregisterChannel(channelId);

            return base.OnDisconnected();
        }

        private bool OnBeginRequest()
        {
            return ChangeTracker.OnBeginRequest();
        }

        private void OnEndRequest()
        {
            ChangeTracker.OnEndRequest();
        }
    }*/
}
