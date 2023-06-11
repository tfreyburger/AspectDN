// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Threading;
using Foundation.Common;
using AspectDN;

namespace AspectDN.Wnd
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        Window _Window;
        string _ProjectFilename;
        string _ProjectFileContent;
        ICommand _OpenAspectDNProjectCommand;
        ICommand _WeaveAspectDNProjectCommand;
        ICommand _ResetAspectDNProjectCommand;
        bool _WeaveAspectDNProject_CanExecute = false;
        ObservableCollection<TaskEvent> _WeaveAspectDNProject_Events;
        string _Events;
        BackgroundWorker _BackgroundWorker;
        bool _ResetAspectDNProject_CanExecute = false;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler WeaveAspectDNProjectEventsChanged;
        public string ProjectFilename
        {
            get => _ProjectFilename;
            set
            {
                _ProjectFilename = value;
                _OnPropertyChanged(nameof(ProjectFilename));
                WeaveAspectDNProject_CanExecute = true;
            }
        }

        public string ProjectFileContent
        {
            get => _ProjectFileContent;
            set
            {
                _ProjectFileContent = value;
                _OnPropertyChanged(nameof(ProjectFileContent));
            }
        }

        public ICommand OpenAspectDNProjectCommand
        {
            get
            {
                return _OpenAspectDNProjectCommand;
            }

            set
            {
                _OpenAspectDNProjectCommand = value;
            }
        }

        public ICommand WeaveAspectDNProjectCommand
        {
            get
            {
                return _WeaveAspectDNProjectCommand;
            }

            set
            {
                _WeaveAspectDNProjectCommand = value;
            }
        }

        public ICommand ResetAspectDNProjectCommand
        {
            get
            {
                return _ResetAspectDNProjectCommand;
            }

            set
            {
                _ResetAspectDNProjectCommand = value;
            }
        }

        public bool WeaveAspectDNProject_CanExecute
        {
            get => _WeaveAspectDNProject_CanExecute;
            set
            {
                _WeaveAspectDNProject_CanExecute = value;
                _OnPropertyChanged(nameof(WeaveAspectDNProject_CanExecute));
            }
        }

        public bool ResetAspectDNProject_CanExecute
        {
            get => _ResetAspectDNProject_CanExecute;
            set
            {
                _ResetAspectDNProject_CanExecute = value;
                _OnPropertyChanged(nameof(ResetAspectDNProject_CanExecute));
            }
        }

        public ObservableCollection<TaskEvent> WeaveAspectDNProject_Events { get => _WeaveAspectDNProject_Events; }

        public MainWindowViewModel(Window window)
        {
            OpenAspectDNProjectCommand = new RelayCommand(new Action<object>(_OpenAspectDNProject_Click), (o) => string.IsNullOrEmpty(ProjectFilename));
            WeaveAspectDNProjectCommand = new RelayCommand(new Action<object>(_WeaveAspectDNProject_Click), (o) => !string.IsNullOrEmpty(ProjectFilename) && WeaveAspectDNProject_CanExecute);
            ResetAspectDNProjectCommand = new RelayCommand(new Action<object>(_ResetAspectDNProject_Click), (o) => ResetAspectDNProject_CanExecute);
            _WeaveAspectDNProject_Events = new ObservableCollection<TaskEvent>();
            TaskEventLogger.EventLogger += _OnEventLogger;
            _BackgroundWorker = new BackgroundWorker();
            _BackgroundWorker.DoWork += _BackgroundWorker_DoWork;
            _BackgroundWorker.RunWorkerCompleted += _BackgroundWorker_RunWorkerCompleted;
            _Window = window;
            _Initialisation();
        }

        private void _BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Weave.Create(_ProjectFilename);
        }

        private void _BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ResetAspectDNProject_CanExecute = true;
            _OnPropertyChanged(nameof(ResetAspectDNProject_CanExecute));
        }

        void _OpenAspectDNProject_Click(object obj)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                ProjectFilename = openFileDialog.FileName;
                ProjectFileContent = File.ReadAllText(openFileDialog.FileName);
            }
        }

        void _WeaveAspectDNProject_Click(object obj)
        {
            WeaveAspectDNProject_CanExecute = false;
            _BackgroundWorker.RunWorkerAsync();
        }

        void _ResetAspectDNProject_Click(object obj)
        {
            _Initialisation();
        }

        void _OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        void _OnEventLogger(object sender, TaskEventArgs eventLoggerArgs)
        {
            var x = eventLoggerArgs.TaskEvent.DateTime;
            _Window.Dispatcher.Invoke(() =>
            {
                WeaveAspectDNProject_Events.Add(eventLoggerArgs.TaskEvent);
            });
            _OnPropertyChanged(nameof(WeaveAspectDNProject_Events));
        }

        void _Initialisation()
        {
            ProjectFilename = null;
            ProjectFileContent = null;
            WeaveAspectDNProject_Events.Clear();
            ResetAspectDNProject_CanExecute = false;
        }
    }
}