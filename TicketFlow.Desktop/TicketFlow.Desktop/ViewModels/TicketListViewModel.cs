using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicketFlow.Desktop.Models;
using TicketFlow.Desktop.Services;

namespace TicketFlow.Desktop.ViewModels
{
    public partial class TicketListViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ApiClient _apiClient;

        [ObservableProperty]
        private ObservableCollection<TicketListResponse> _tickets = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public bool IsUserRole => SessionManager.Instance.Role != "Admin";

        public TicketListViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _apiClient = new ApiClient();
            LoadTicketsCommand.Execute(null);
        }

        [RelayCommand]
        public async Task LoadTicketsAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var response = await _apiClient.GetAsync<System.Collections.Generic.IEnumerable<TicketListResponse>>("tickets");

                if (response != null && response.Success && response.Data != null)
                {
                    Tickets = new ObservableCollection<TicketListResponse>(response.Data);
                }
                else
                {
                    ErrorMessage = response?.Message ?? "Failed to load tickets.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void NavigateToCreateTicket()
        {
            _mainViewModel.NavigateTo(new CreateTicketViewModel(_mainViewModel));
        }

        [RelayCommand]
        public void ViewTicketDetail(int ticketId)
        {
            _mainViewModel.NavigateTo(new TicketDetailViewModel(_mainViewModel, ticketId));
        }
    }
}
