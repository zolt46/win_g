// File: PublicPCControl.Client/ViewModels/AdminViewModel.cs
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using PublicPCControl.Client.Models;
using PublicPCControl.Client.Services;
using System.Diagnostics;
using System.Drawing;
using System;

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

        public ObservableCollection<AllowedProgram> AllowedPrograms { get; } = new();
        public ObservableCollection<RunningProgramOption> RunningPrograms { get; } = new();

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

        public ICommand AddProgramCommand { get; }
        public ICommand RemoveProgramCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand EnterMaintenanceCommand { get; }
        public ICommand ResumeFromMaintenanceCommand { get; }
        public ICommand RefreshRunningProgramsCommand { get; }
        public ICommand AddFromRunningProgramCommand { get; }

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
            RefreshRunningProgramsCommand = new RelayCommand(_ => LoadRunningPrograms());
            AddFromRunningProgramCommand = new RelayCommand(p => AddFromRunningProgram(p as RunningProgramOption), p => p is RunningProgramOption);
        }

        public void Refresh(AppConfig config)
        {
            _config = config;
            AllowedPrograms.Clear();
            foreach (var program in _config.AllowedPrograms)
            {
                AllowedPrograms.Add(program);
            }
            LoadRunningPrograms();
            OnPropertyChanged(string.Empty);
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

            var program = new AllowedProgram { DisplayName = NewProgramName, ExecutablePath = NewProgramPath };
            AllowedPrograms.Add(program);
            _config.AllowedPrograms = AllowedPrograms.ToList();
            NewProgramName = string.Empty;
            NewProgramPath = string.Empty;
        }

        private void AddFromRunningProgram(RunningProgramOption? option)
        {
            if (option == null) return;
            NewProgramName = option.Name;
            NewProgramPath = option.Path;
            AddProgram();
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

        private void LoadRunningPrograms()
        {
            RunningPrograms.Clear();
            try
            {
                var existingPaths = AllowedPrograms.Select(p => p.ExecutablePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var process in Process.GetProcesses().OrderBy(p => p.ProcessName))
                {
                    try
                    {
                        var path = process.MainModule?.FileName;
                        if (string.IsNullOrWhiteSpace(path) || existingPaths.Contains(path))
                        {
                            continue;
                        }

                        RunningPrograms.Add(new RunningProgramOption
                        {
                            Name = process.ProcessName,
                            Path = path,
                            Icon = TryLoadIcon(path)
                        });
                    }
                    catch
                    {
                        // 접근 불가 프로세스는 무시
                    }
                }
            }
            catch
            {
                // 프로세스 열람 실패 시 조용히 무시
            }
        }

        private static ImageSource? TryLoadIcon(string path)
        {
            try
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon == null)
                {
                    return null;
                }

                return Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(48, 48));
            }
            catch
            {
                return null;
            }
        }

        private void RefreshProgramCommands()
        {
            if (AddProgramCommand is RelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }
    }

    public class RunningProgramOption
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public ImageSource? Icon { get; set; }
    }
}
