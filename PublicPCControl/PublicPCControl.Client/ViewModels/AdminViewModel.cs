// File: PublicPCControl.Client/ViewModels/AdminViewModel.cs
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
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
        private AppConfig _config = new();
        private string _newProgramName = string.Empty;
        private string _newProgramPath = string.Empty;
        private string _newAdminPassword = string.Empty;
        private string _confirmAdminPassword = string.Empty;

        public ObservableCollection<AllowedProgram> AllowedPrograms { get; } = new();

        public bool EnforcementEnabled
        {
            get => _config.EnforcementEnabled;
            set { _config.EnforcementEnabled = value; OnPropertyChanged(); }
        }

        public bool IsAdminOnlyPc
        {
            get => _config.IsAdminOnlyPc;
            set { _config.IsAdminOnlyPc = value; OnPropertyChanged(); }
        }

        public int DefaultSessionMinutes
        {
            get => _config.DefaultSessionMinutes;
            set { _config.DefaultSessionMinutes = value; OnPropertyChanged(); }
        }

        public int MaxSessionMinutes
        {
            get => _config.MaxSessionMinutes;
            set { _config.MaxSessionMinutes = value; OnPropertyChanged(); }
        }

        public bool KillDisallowedProcess
        {
            get => _config.KillDisallowedProcess;
            set { _config.KillDisallowedProcess = value; OnPropertyChanged(); }
        }

        public string NewProgramName
        {
            get => _newProgramName;
            set => SetProperty(ref _newProgramName, value);
        }

        public string NewProgramPath
        {
            get => _newProgramPath;
            set => SetProperty(ref _newProgramPath, value);
        }
        public string NewAdminPassword
        {
            get => _newAdminPassword;
            set => SetProperty(ref _newAdminPassword, value);
        }

        public string ConfirmAdminPassword
        {
            get => _confirmAdminPassword;
            set => SetProperty(ref _confirmAdminPassword, value);
        }


        public ICommand AddProgramCommand { get; }
        public ICommand RemoveProgramCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CloseCommand { get; }

        public AdminViewModel(ConfigService configService, System.Action<AppConfig> saveCallback, System.Action close)
        {
            _configService = configService;
            _saveCallback = saveCallback;
            _close = close;
            AddProgramCommand = new RelayCommand(_ => AddProgram(), _ => !string.IsNullOrWhiteSpace(NewProgramName) && !string.IsNullOrWhiteSpace(NewProgramPath));
            RemoveProgramCommand = new RelayCommand(p => RemoveProgram(p as AllowedProgram), p => p is AllowedProgram);
            SaveCommand = new RelayCommand(_ => Save());
            CloseCommand = new RelayCommand(_ => _close());
        }

        public void Refresh(AppConfig config)
        {
            _config = config;
            AllowedPrograms.Clear();
            foreach (var program in _config.AllowedPrograms)
            {
                AllowedPrograms.Add(program);
            }
            OnPropertyChanged(string.Empty);
        }

        private void AddProgram()
        {
            var program = new AllowedProgram { DisplayName = NewProgramName, ExecutablePath = NewProgramPath };
            AllowedPrograms.Add(program);
            _config.AllowedPrograms = AllowedPrograms.ToList();
            NewProgramName = string.Empty;
            NewProgramPath = string.Empty;
        }

        private void RemoveProgram(AllowedProgram? program)
        {
            if (program == null) return;
            AllowedPrograms.Remove(program);
            _config.AllowedPrograms = AllowedPrograms.ToList();
        }

        private void Save()
        {
            _config.AllowedPrograms = AllowedPrograms.ToList();
            if (!string.IsNullOrWhiteSpace(NewAdminPassword))
            {
                if (!string.Equals(NewAdminPassword, ConfirmAdminPassword))
                {
                    MessageBox.Show("비밀번호와 확인 값이 일치하지 않습니다.", "비밀번호 설정 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _config.AdminPasswordHash = ConfigService.HashPassword(NewAdminPassword);
                NewAdminPassword = string.Empty;
                ConfirmAdminPassword = string.Empty;
            }
            _saveCallback(_config);
            _configService.Save(_config);
        }
    }
}