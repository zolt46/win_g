// File: PublicPCControl.Client/ViewModels/AdminViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using PublicPCControl.Client.Models;
using PublicPCControl.Client.Services;
using PublicPCControl.Client.Views;

namespace PublicPCControl.Client.ViewModels
{
    public class AdminViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly System.Action<AppConfig> _saveCallback;
        private readonly System.Action _close;
        private readonly System.Action _enterMaintenance;
        private readonly System.Action _resumeFromMaintenance;
        private readonly System.Func<bool> _isMaintenanceActive;
        private AppConfig _config = new();
        private bool _isRefreshing;
        private bool _hasUnsavedChanges;
        private string _newProgramName = string.Empty;
        private string _newProgramPath = string.Empty;
        private string _newProgramArguments = string.Empty;
        private string _programSearchText = string.Empty;

        public ObservableCollection<AllowedProgram> AllowedPrograms { get; } = new();

        public ICollectionView AllowedProgramsView { get; }

        public bool EnforcementEnabled
        {
            get => _config.EnforcementEnabled;
            set
            {
                _config.EnforcementEnabled = value;
                if (value)
                {
                    _config.IsAdminOnlyPc = false;
                    OnPropertyChanged(nameof(IsAdminOnlyPc));
                }
                else if (!_config.IsAdminOnlyPc)
                {
                    _config.IsAdminOnlyPc = true;
                    OnPropertyChanged(nameof(IsAdminOnlyPc));
                }
                MarkDirty();
                OnPropertyChanged();
            }
        }

        public bool IsAdminOnlyPc
        {
            get => _config.IsAdminOnlyPc;
            set
            {
                _config.IsAdminOnlyPc = value;
                if (value)
                {
                    _config.EnforcementEnabled = false;
                    OnPropertyChanged(nameof(EnforcementEnabled));
                }
                else if (!_config.EnforcementEnabled)
                {
                    _config.EnforcementEnabled = true;
                    OnPropertyChanged(nameof(EnforcementEnabled));
                }
                MarkDirty();
                OnPropertyChanged();
            }
        }

        public int DefaultSessionMinutes
        {
            get => _config.DefaultSessionMinutes;
            set { _config.DefaultSessionMinutes = value; MarkDirty(); OnPropertyChanged(); }
        }

        public int SessionExtensionMinutes
        {
            get => _config.SessionExtensionMinutes;
            set { _config.SessionExtensionMinutes = value; MarkDirty(); OnPropertyChanged(); }
        }

        public int MaxExtensionCount
        {
            get => _config.MaxExtensionCount;
            set { _config.MaxExtensionCount = value; MarkDirty(); OnPropertyChanged(); }
        }

        public bool AllowExtensions
        {
            get => _config.AllowExtensions;
            set { _config.AllowExtensions = value; MarkDirty(); OnPropertyChanged(); }
        }

        public bool KillDisallowedProcess
        {
            get => _config.KillDisallowedProcess;
            set { _config.KillDisallowedProcess = value; MarkDirty(); OnPropertyChanged(); }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public string NewProgramName
        {
            get => _newProgramName;
            set
            {
                if (SetProperty(ref _newProgramName, value))
                {
                    RefreshProgramCommands();
                }
            }
        }

        public string NewProgramPath
        {
            get => _newProgramPath;
            set
            {
                if (SetProperty(ref _newProgramPath, value))
                {
                    RefreshProgramCommands();
                }
            }
        }

        public string NewProgramArguments
        {
            get => _newProgramArguments;
            set => SetProperty(ref _newProgramArguments, value);
        }

        public string ProgramSearchText
        {
            get => _programSearchText;
            set
            {
                if (SetProperty(ref _programSearchText, value))
                {
                    AllowedProgramsView.Refresh();
                }
            }
        }

        public ICommand AddProgramCommand { get; }
        public ICommand RemoveProgramCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand EnterMaintenanceCommand { get; }
        public ICommand ResumeFromMaintenanceCommand { get; }
        public ICommand OpenSuggestionsCommand { get; }

        public AdminViewModel(
            ConfigService configService,
            System.Action<AppConfig> saveCallback,
            System.Action close,
            System.Action enterMaintenance,
            System.Action resumeFromMaintenance,
            System.Func<bool> isMaintenanceActive)
        {
            _configService = configService;
            _saveCallback = saveCallback;
            _close = close;
            _enterMaintenance = enterMaintenance;
            _resumeFromMaintenance = resumeFromMaintenance;
            _isMaintenanceActive = isMaintenanceActive;
            AddProgramCommand = new RelayCommand(_ => AddProgram(), _ => !string.IsNullOrWhiteSpace(NewProgramName) && !string.IsNullOrWhiteSpace(NewProgramPath));
            RemoveProgramCommand = new RelayCommand(p => RemoveProgram(p as AllowedProgram), p => p is AllowedProgram);
            SaveCommand = new RelayCommand(_ => Save());
            CloseCommand = new RelayCommand(_ => CloseWithSave());
            EnterMaintenanceCommand = new RelayCommand(_ => _enterMaintenance());
            ResumeFromMaintenanceCommand = new RelayCommand(_ => _resumeFromMaintenance(), _ => _isMaintenanceActive());
            OpenSuggestionsCommand = new RelayCommand(_ => OpenSuggestions());

            AllowedProgramsView = CollectionViewSource.GetDefaultView(AllowedPrograms);
            AllowedProgramsView.Filter = FilterPrograms;
        }

        public void Refresh(AppConfig config)
        {
            _isRefreshing = true;
            _config = config;
            AllowedPrograms.Clear();
            foreach (var program in _config.AllowedPrograms)
            {
                program.Icon ??= IconHelper.LoadIcon(program.ExecutablePath);
                AllowedPrograms.Add(program);
            }
            OnPropertyChanged(string.Empty);
            EnsureModeSelected();
            HasUnsavedChanges = false;
            _isRefreshing = false;
            if (ResumeFromMaintenanceCommand is RelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }

        public void NotifyMaintenanceStateChanged()
        {
            if (ResumeFromMaintenanceCommand is RelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }

        private void AddProgram()
        {
            if (TryAddProgram(new AllowedProgram
            {
                DisplayName = NewProgramName,
                ExecutablePath = NewProgramPath,
                Arguments = NewProgramArguments
            }, true))
            {
                NewProgramName = string.Empty;
                NewProgramPath = string.Empty;
                NewProgramArguments = string.Empty;
            }
        }

        public void Save()
        {
            _config.AllowedPrograms = AllowedPrograms.ToList();
            if (_config.DefaultSessionMinutes < 1)
            {
                _config.DefaultSessionMinutes = 1;
            }
            if (_config.SessionExtensionMinutes < 0)
            {
                _config.SessionExtensionMinutes = 0;
            }
            if (_config.MaxExtensionCount < 0)
            {
                _config.MaxExtensionCount = 0;
            }
            _saveCallback(_config);
            _configService.Save(_config);
            HasUnsavedChanges = false;
        }

        public void ApplyNewAdminPassword(string password)
        {
            _config.AdminPasswordHash = ConfigService.HashPassword(password);
            _saveCallback(_config);
            _configService.Save(_config);
            HasUnsavedChanges = false;
        }

        private void RemoveProgram(AllowedProgram? program)
        {
            if (program == null) return;
            AllowedPrograms.Remove(program);
            _config.AllowedPrograms = AllowedPrograms.ToList();
            MarkDirty();
        }

        public bool ApplySuggestion(ProgramSuggestion? suggestion, bool showMessage = true)
        {
            if (suggestion == null)
            {
                return false;
            }

            return TryAddProgram(new AllowedProgram
            {
                DisplayName = suggestion.DisplayName,
                ExecutablePath = suggestion.ExecutablePath,
                Arguments = string.Empty
            }, showMessage);
        }

        private void RefreshProgramCommands()
        {
            if (AddProgramCommand is RelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }

        private void CloseWithSave()
        {
            if (HasUnsavedChanges)
            {
                Save();
            }

            _close();
        }

        private void MarkDirty()
        {
            if (_isRefreshing)
            {
                return;
            }

            HasUnsavedChanges = true;
        }

        private void CloseWithSave()
        {
            if (HasUnsavedChanges)
            {
                Save();
            }

            _close();
        }

        private void MarkDirty()
        {
            if (_isRefreshing)
            {
                return;
            }

            HasUnsavedChanges = true;
        }

        private void EnsureModeSelected()
        {
            if (!_config.EnforcementEnabled && !_config.IsAdminOnlyPc)
            {
                _config.EnforcementEnabled = true;
                OnPropertyChanged(nameof(EnforcementEnabled));
            }
        }

        public bool IsAlreadyAllowed(string executablePath)
        {
            return AllowedPrograms.Any(p => string.Equals(p.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase));
        }

        private bool TryAddProgram(AllowedProgram program, bool showMessages)
        {
            if (!File.Exists(program.ExecutablePath))
            {
                if (showMessages)
                {
                    MessageBox.Show("실행 파일 경로가 존재하지 않습니다.", "경로 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                return false;
            }

            if (IsAlreadyAllowed(program.ExecutablePath))
            {
                if (showMessages)
                {
                    MessageBox.Show("이미 동일한 경로가 허용 목록에 있습니다.", "중복 추가", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return false;
            }

            program.Icon ??= IconHelper.LoadIcon(program.ExecutablePath);
            AllowedPrograms.Add(program);
            _config.AllowedPrograms = AllowedPrograms.ToList();
            MarkDirty();
            return true;
        }

        private void OpenSuggestions()
        {
            var window = new Views.ProgramSuggestionsWindow();
            var viewModel = new ProgramSuggestionsViewModel(
                ProgramDiscoveryService.FindSuggestions,
                suggestion => ApplySuggestion(suggestion, true),
                suggestion => !IsAlreadyAllowed(suggestion.ExecutablePath));
            window.DataContext = viewModel;
            window.Owner = Application.Current?.MainWindow;
            window.ShowDialog();
        }

        public bool IsAlreadyAllowed(string executablePath)
        {
            return AllowedPrograms.Any(p => string.Equals(p.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase));
        }

        private bool TryAddProgram(AllowedProgram program, bool showMessages)
        {
            if (!File.Exists(program.ExecutablePath))
            {
                if (showMessages)
                {
                    MessageBox.Show("실행 파일 경로가 존재하지 않습니다.", "경로 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                return false;
            }

            if (IsAlreadyAllowed(program.ExecutablePath))
            {
                if (showMessages)
                {
                    MessageBox.Show("이미 동일한 경로가 허용 목록에 있습니다.", "중복 추가", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return false;
            }

            program.Icon ??= IconHelper.LoadIcon(program.ExecutablePath);
            AllowedPrograms.Add(program);
            _config.AllowedPrograms = AllowedPrograms.ToList();
            MarkDirty();
            return true;
        }

        private void OpenSuggestions()
        {
            var window = new Views.ProgramSuggestionsWindow();
            var viewModel = new ProgramSuggestionsViewModel(
                ProgramDiscoveryService.FindSuggestions,
                suggestion => ApplySuggestion(suggestion, true),
                suggestion => !IsAlreadyAllowed(suggestion.ExecutablePath));
            window.DataContext = viewModel;
            window.Owner = Application.Current?.MainWindow;
            window.ShowDialog();
        }
    }
}
