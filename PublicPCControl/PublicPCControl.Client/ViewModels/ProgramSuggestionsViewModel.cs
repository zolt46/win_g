// File: PublicPCControl.Client/ViewModels/ProgramSuggestionsViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using PublicPCControl.Client.Models;
using PublicPCControl.Client.Services;

namespace PublicPCControl.Client.ViewModels
{
    public class ProgramSuggestionsViewModel : ViewModelBase
    {
        private readonly Func<IEnumerable<ProgramSuggestion>> _loader;
        private readonly Func<ProgramSuggestion, bool> _apply;
        private readonly Func<ProgramSuggestion, bool> _canDisplay;
        private string _searchText = string.Empty;

        public ObservableCollection<ProgramSuggestion> Suggestions { get; } = new();

        public ICollectionView SuggestionsView { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    SuggestionsView.Refresh();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand UseSuggestionCommand { get; }

        public ProgramSuggestionsViewModel(
            Func<IEnumerable<ProgramSuggestion>> loader,
            Func<ProgramSuggestion, bool> apply,
            Func<ProgramSuggestion, bool> canDisplay)
        {
            _loader = loader;
            _apply = apply;
            _canDisplay = canDisplay;
            RefreshCommand = new RelayCommand(_ => LoadSuggestions());
            UseSuggestionCommand = new RelayCommand(p => ApplySuggestion(p as ProgramSuggestion), p => p is ProgramSuggestion);

            SuggestionsView = CollectionViewSource.GetDefaultView(Suggestions);
            SuggestionsView.Filter = FilterSuggestion;

            LoadSuggestions();
        }

        private void LoadSuggestions()
        {
            Suggestions.Clear();
            try
            {
                foreach (var suggestion in _loader().Where(_canDisplay))
                {
                    Suggestions.Add(suggestion);
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.Log("ProgramSuggestions", ex);
            }

            SuggestionsView.Refresh();
        }

        private void ApplySuggestion(ProgramSuggestion? suggestion)
        {
            if (suggestion == null)
            {
                return;
            }

            if (_apply(suggestion))
            {
                Suggestions.Remove(suggestion);
            }
        }

        private bool FilterSuggestion(object obj)
        {
            if (obj is not ProgramSuggestion suggestion)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            return suggestion.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                   || suggestion.ExecutablePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
