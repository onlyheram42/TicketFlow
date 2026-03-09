using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicketFlow.Desktop.Models;
using TicketFlow.Desktop.Services;

namespace TicketFlow.Desktop.ViewModels
{
    public partial class TicketDetailViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ApiClient _apiClient;
        private readonly int _ticketId;

        [ObservableProperty]
        private TicketDetailResponse? _ticket;

        [ObservableProperty]
        private ObservableCollection<AdminUserResponse> _adminUsers = new();

        [ObservableProperty]
        private AdminUserResponse? _selectedAdmin;

        [ObservableProperty]
        private string _newComment = string.Empty;

        [ObservableProperty]
        private bool _isInternalComment;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public bool IsAdmin => SessionManager.Instance.IsAdmin;
        
        public List<string> StatusOptions { get; } = new() { "Open", "InProgress", "Closed" };
        
        [ObservableProperty]
        private string _selectedStatus = string.Empty;

        public TicketDetailViewModel(MainViewModel mainViewModel, int ticketId)
        {
            _mainViewModel = mainViewModel;
            _ticketId = ticketId;
            _apiClient = new ApiClient();
            
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var response = await _apiClient.GetAsync<TicketDetailResponse>($"tickets/{_ticketId}");
                if (response != null && response.Success && response.Data != null)
                {
                    Ticket = response.Data;
                    SelectedStatus = Ticket.Status;

                    if (IsAdmin)
                    {
                        var adminsResponse = await _apiClient.GetAsync<IEnumerable<AdminUserResponse>>("tickets/admins");
                        if (adminsResponse != null && adminsResponse.Success && adminsResponse.Data != null)
                        {
                            AdminUsers = new ObservableCollection<AdminUserResponse>(adminsResponse.Data);
                            
                            // Find assigned admin in the list
                            foreach (var admin in AdminUsers)
                            {
                                if (admin.FullName == Ticket.AssignedToName)
                                {
                                    SelectedAdmin = admin;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    ErrorMessage = response?.Message ?? "Failed to load ticket details.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Network Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task UpdateStatusAsync()
        {
            if (Ticket == null || SelectedStatus == Ticket.Status) return;

            try
            {
                var request = new UpdateTicketStatusRequest { Status = SelectedStatus };
                var response = await _apiClient.PutAsync($"tickets/{_ticketId}/status", request);
                
                if (response.Success)
                {
                    await LoadDataAsync(); // Reload to get updated history
                }
                else
                {
                    ErrorMessage = response.Message;
                    SelectedStatus = Ticket.Status; // Revert selection
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating status: {ex.Message}";
                SelectedStatus = Ticket.Status;
            }
        }

        [RelayCommand]
        public async Task AssignTicketAsync()
        {
            if (Ticket == null || SelectedAdmin == null || SelectedAdmin.FullName == Ticket.AssignedToName) return;

            try
            {
                var request = new AssignTicketRequest { AssignToUserId = SelectedAdmin.Id };
                var response = await _apiClient.PutAsync($"tickets/{_ticketId}/assign", request);
                
                if (response.Success)
                {
                    await LoadDataAsync(); // Reload
                }
                else
                {
                    ErrorMessage = response.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error assigning ticket: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task AddCommentAsync()
        {
            if (string.IsNullOrWhiteSpace(NewComment)) return;

            try
            {
                var request = new AddCommentRequest 
                { 
                    Comment = NewComment, 
                    IsInternal = IsInternalComment 
                };
                
                var response = await _apiClient.PostNoDataAsync($"tickets/{_ticketId}/comments", request);
                
                if (response.Success)
                {
                    NewComment = string.Empty;
                    IsInternalComment = false;
                    await LoadDataAsync(); // Reload to get new comments
                }
                else
                {
                    ErrorMessage = response.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error adding comment: {ex.Message}";
            }
        }

        [RelayCommand]
        public void BackToList()
        {
            _mainViewModel.NavigateTo(new TicketListViewModel(_mainViewModel));
        }
    }
}
