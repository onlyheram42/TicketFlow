using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicketFlow.Desktop.Models;
using TicketFlow.Desktop.Services;

namespace TicketFlow.Desktop.ViewModels
{
    public partial class CreateTicketViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ApiClient _apiClient;

        [ObservableProperty]
        private string _subject = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _selectedPriority = "Medium";

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public List<string> Priorities { get; } = new() { "Low", "Medium", "High", "Critical" };

        public CreateTicketViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _apiClient = new ApiClient();
        }

        [RelayCommand]
        public async Task CreateTicketAsync()
        {
            if (string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(Description))
            {
                ErrorMessage = "Subject and Description are required.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var request = new CreateTicketRequest
                {
                    Subject = Subject,
                    Description = Description,
                    Priority = SelectedPriority
                };

                var response = await _apiClient.PostAsync<object>("tickets", request);

                if (response != null && response.Success)
                {
                    _mainViewModel.NavigateTo(new TicketListViewModel(_mainViewModel));
                }
                else
                {
                    ErrorMessage = response?.Message ?? "Failed to create ticket.";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Network Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            _mainViewModel.NavigateTo(new TicketListViewModel(_mainViewModel));
        }
    }
}
