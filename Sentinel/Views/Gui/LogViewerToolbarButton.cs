﻿namespace Sentinel.Views.Gui
{
    using System.Diagnostics;
    using System.Windows.Input;

    using Sentinel.Support.Mvvm;
    using Sentinel.Views.Interfaces;

    public class LogViewerToolbarButton
        : ViewModelBase, ILogViewerToolbarButton
    {
        private string imageIdentifier;

        private bool isChecked;

        public LogViewerToolbarButton(
            string label,
            string toolTip,
            bool checkable,
            ICommand command)
        {
            Tooltip = toolTip;
            Label = label;
            CanCheck = checkable;
            Command = command;
        }

        public bool CanCheck { get; private set; }

        public ICommand Command { get; private set; }

        public string ImageIdentifier
        {
            get
            {
                return imageIdentifier;
            }

            set
            {
                if (imageIdentifier != value)
                {
                    imageIdentifier = value;
                    OnPropertyChanged("ImageIdentifier");
                }
            }
        }

        public bool IsChecked
        {
            get
            {
                return isChecked;
            }

            set
            {
                Debug.Assert(CanCheck, "Should not be able to check a non-checkable button, so why look?");

                if (isChecked != value)
                {
                    isChecked = value;
                    OnPropertyChanged("IsChecked");
                }
            }
        }

        public string Label { get; private set; }

        public string Tooltip { get; private set; }
    }
}