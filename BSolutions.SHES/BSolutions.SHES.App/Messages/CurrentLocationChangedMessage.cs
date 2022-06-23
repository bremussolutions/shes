﻿using BSolutions.SHES.Models.Entities;
using BSolutions.SHES.Models.Observables;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BSolutions.SHES.App.Messages
{
    public class CurrentLocationChangedMessage : ValueChangedMessage<ObservableProjectItem>
    {
        public CurrentLocationChangedMessage(ObservableProjectItem projectItem)
            : base(projectItem)
        {
        }
    }
}