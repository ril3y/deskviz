using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DeskViz.Plugins.Interfaces;

namespace DeskViz.Plugins.Base
{
    public abstract class BaseWidgetSettingsViewModel<TSettings> : INotifyPropertyChanged
        where TSettings : BaseWidgetSettings, new()
    {
        private TSettings _settings = new();
        private TSettings _originalSettings = new();
        private ICommand? _saveCommand;
        private ICommand? _cancelCommand;
        private ICommand? _resetCommand;

        public TSettings Settings
        {
            get => _settings;
            set
            {
                if (_settings != value)
                {
                    if (_settings != null)
                        _settings.PropertyChanged -= OnSettingsPropertyChanged;

                    _settings = value;

                    if (_settings != null)
                        _settings.PropertyChanged += OnSettingsPropertyChanged;

                    OnPropertyChanged(nameof(Settings));
                    OnPropertyChanged(nameof(HasChanges));
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public bool HasChanges => !_settings.Equals(_originalSettings);

        public bool IsValid => _settings.Validate();

        public string[] ValidationErrors => _settings.GetValidationErrors();

        public ICommand SaveCommand => _saveCommand ??= new RelayCommand(_ => Save(), _ => CanSave());

        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(_ => Cancel());

        public ICommand ResetCommand => _resetCommand ??= new RelayCommand(_ => Reset());

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<SettingsEventArgs>? SettingsSaved;
        public event EventHandler? SettingsCancelled;
        public event EventHandler? SettingsReset;

        protected BaseWidgetSettingsViewModel()
        {
            _settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        public void LoadSettings(TSettings settings)
        {
            _originalSettings = (TSettings)settings.Clone();
            Settings = (TSettings)settings.Clone();
        }

        protected virtual bool CanSave()
        {
            return HasChanges && IsValid;
        }

        protected virtual void Save()
        {
            if (!CanSave()) return;

            SettingsSaved?.Invoke(this, new SettingsEventArgs(_settings));
        }

        protected virtual void Cancel()
        {
            Settings = (TSettings)_originalSettings.Clone();
            SettingsCancelled?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void Reset()
        {
            Settings.Reset();
            SettingsReset?.Invoke(this, EventArgs.Empty);
        }

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasChanges));
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(ValidationErrors));

            // Auto-save for live updates - apply changes immediately if valid
            if (IsValid && HasChanges)
            {
                AutoSave();
            }
        }

        /// <summary>
        /// Automatically saves settings for live updates
        /// </summary>
        protected virtual void AutoSave()
        {
            try
            {
                SettingsSaved?.Invoke(this, new SettingsEventArgs(_settings));
            }
            catch (Exception ex)
            {
                // Log error but don't throw - keep UI responsive
                System.Diagnostics.Debug.WriteLine($"Auto-save error: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SettingsEventArgs : EventArgs
    {
        public object Settings { get; }

        public SettingsEventArgs(object settings)
        {
            Settings = settings;
        }
    }
}