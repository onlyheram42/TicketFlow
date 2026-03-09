using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicketFlow.Desktop.Models;
using TicketFlow.Desktop.Services;

namespace TicketFlow.Desktop.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ApiClient _apiClient;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public LoginViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _apiClient = new ApiClient();
        }

        [RelayCommand]
        public async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both username and password.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var request = new LoginRequest { Username = Username, Password = Password };
                var response = await _apiClient.PostAsync<AuthResponse>("auth/login", request);

                if (response != null && response.Success && response.Data != null)
                {
                    // Update global session with token and user details
                    SessionManager.Instance.Login(response.Data.Token, response.Data.FullName, response.Data.Role);

                    // Navigate to Ticket List
                    _mainViewModel.NavigateTo(new TicketListViewModel(_mainViewModel));
                }
                else
                {
                    ErrorMessage = response?.Message ?? "Login failed. Please check your credentials.";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"A network error occurred. Ensure the API is running. ({ex.Message})";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
