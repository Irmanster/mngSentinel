﻿namespace Sentinel.Services.Interfaces
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Windows;

    using Sentinel.Interfaces.Providers;
    using Sentinel.Providers.Interfaces;
    using Sentinel.Support.Mvvm;

    public interface ISessionManager
    {
        [DataMember]
        string Name { get; }

        [DataMember]
        IEnumerable<IProviderSettings> ProviderSettings { get; }

        IEnumerable<ViewModelBase> ChangingViewModelBases { get; set; }

        bool IsSaved { get; set; }

        /// <summary>
        /// Called when loading a brand new session.
        /// </summary>
        void LoadNewSession(Window parent);

        /// <summary>
        /// Initialise the session manager from supplied providers
        /// </summary>
        /// <param name="providers"></param>
        void LoadProviders(IEnumerable<PendingProviderRecord> providers);

        /// <summary>
        /// Called when loading a session that has been saved to disk.
        /// </summary>
        /// <param name="filePath"></param>
        void LoadSession(string filePath);

        void SaveSession(string filePath);

    }
}
