// File: PublicPCControl.Client/ViewModels/AdminViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using PublicPCControl.Client.Models;
using PublicPCControl.Client.Services;

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
        private string _newProgramName = string.Empty;
        private string _newProgramPath = string.Empty;
        private string _newProgramArguments = string.Empty;
        private string _programSearchText = string.Empty;

        public ObservableCollection<AllowedProgram> AllowedPrograms { get; } = new();
        public ObservableCollection<ProgramSuggestion> ProgramSuggestions { get; } = new();

        public ICollectionView AllowedProgramsView { get; }
        public ICollectionView ProgramSuggestionsView { get; }

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
                OnPropertyChanged();
            }
        }

        public int DefaultSessionMinutes
        {
            get => _config.DefaultSessionMinutes;
            set { _config.DefaultSessionMinutes = value; OnPropertyChanged(); }
        }

        public int SessionExtensionMinutes
        {
            get => _config.SessionExtensionMinutes;
            set { _config.SessionExtensionMinutes = value; OnPropertyChanged(); }
        }

        public int MaxExtensionCount
        {
            get => _config.MaxExtensionCount;
            set { _config.MaxExtensionCount = value; OnPropertyChanged(); }
        }

        public bool AllowExtensions
        {
            get => _config.AllowExtensions;
            set { _config.AllowExtensions = value; OnPropertyChanged(); }
        }

        public bool KillDisallowedProcess
        {
            get => _config.KillDisallowedProcess;
            set { _config.KillDisallowedProcess = value; OnPropertyChanged(); }
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
                    ProgramSuggestionsView.Refresh();
                }
            }
        }

        public ICommand AddProgramCommand { get; }
        public ICommand RemoveProgramCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand EnterMaintenanceCommand { get; }
        public ICommand ResumeFromMaintenanceCommand { get; }
        public ICommand UseSuggestionCommand { get; }
        public ICommand RefreshSuggestionsCommand { get; }

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
            CloseCommand = new RelayCommand(_ => _close());
            EnterMaintenanceCommand = new RelayCommand(_ => _enterMaintenance());
            ResumeFromMaintenanceCommand = new RelayCommand(_ => _resumeFromMaintenance(), _ => _isMaintenanceActive());
            RefreshSuggestionsCommand = new RelayCommand(_ => LoadProgramSuggestions());

            AllowedProgramsView = CollectionViewSource.GetDefaultView(AllowedPrograms);
            AllowedProgramsView.Filter = FilterPrograms;

            ProgramSuggestionsView = CollectionViewSource.GetDefaultView(ProgramSuggestions);
            ProgramSuggestionsView.Filter = FilterSuggestions;

            UseSuggestionCommand = new RelayCommand(p => ApplySuggestion(p as ProgramSuggestion), p => p is ProgramSuggestion);
        }

        public void Refresh(AppConfig config)
        {
            _config = config;
            AllowedPrograms.Clear();
            foreach (var program in _config.AllowedPrograms)
            {
                program.Icon ??= IconHelper.LoadIcon(program.ExecutablePath);
                AllowedPrograms.Add(program);
            }
            OnPropertyChanged(string.Empty);
            EnsureModeSelected();
            LoadProgramSuggestions();
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
            if (!File.Exists(NewProgramPath))
            {
                MessageBox.Show("실행 파일 경로가 존재하지 않습니다.", "경로 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AllowedPrograms.Any(p => string.Equals(p.ExecutablePath, NewProgramPath, System.StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("이미 동일한 경로가 허용 목록에 있습니다.", "중복 추가", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var program = new AllowedProgram { DisplayName = NewProgramName, ExecutablePath = NewProgramPath, Arguments = NewProgramArguments };
            program.Icon ??= IconHelper.LoadIcon(program.ExecutablePath);
            AllowedPrograms.Add(program);
            _config.AllowedPrograms = AllowedPrograms.ToList();
            NewProgramName = string.Empty;
            NewProgramPath = string.Empty;
            NewProgramArguments = string.Empty;
        }

        public void Save()
        {
            _config.AllowedPrograms = AllowedPrograms.ToList();
            if (_config.DefaultSessionMinutes < 5)
            {
                _config.DefaultSessionMinutes = 5;
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
        }

        public void ApplyNewAdminPassword(string password)
        {
            _config.AdminPasswordHash = ConfigService.HashPassword(password);
            _saveCallback(_config);
            _configService.Save(_config);
        }

        private void RemoveProgram(AllowedProgram? program)
        {
            if (program == null) return;
            AllowedPrograms.Remove(program);
            _config.AllowedPrograms = AllowedPrograms.ToList();
        }

        private void ApplySuggestion(ProgramSuggestion? suggestion)
        {
            if (suggestion == null)
            {
                return;
            }

            NewProgramName = suggestion.DisplayName;
            NewProgramPath = suggestion.ExecutablePath;
            AddProgram();
        }

        private void RefreshProgramCommands()
        {
            if (AddProgramCommand is RelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }

        private void LoadProgramSuggestions()
        {
            ProgramSuggestions.Clear();
            foreach (var suggestion in ProgramDiscoveryService.FindSuggestions())
            {
                ProgramSuggestions.Add(suggestion);
            }
            ProgramSuggestionsView.Refresh();
        }

        private bool FilterPrograms(object obj)
        {
            if (obj is not AllowedProgram program)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(ProgramSearchText))
            {
                return true;
            }

            return program.DisplayName.Contains(ProgramSearchText, System.StringComparison.OrdinalIgnoreCase)
                   || program.ExecutablePath.Contains(ProgramSearchText, System.StringComparison.OrdinalIgnoreCase);
        }

        private bool FilterSuggestions(object obj)
        {
            if (obj is not ProgramSuggestion suggestion)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(ProgramSearchText))
            {
                return true;
            }

            return suggestion.DisplayName.Contains(ProgramSearchText, System.StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureModeSelected()
        {
            if (!_config.EnforcementEnabled && !_config.IsAdminOnlyPc)
            {
                _config.EnforcementEnabled = true;
                OnPropertyChanged(nameof(EnforcementEnabled));
            }
        }
    }
}
