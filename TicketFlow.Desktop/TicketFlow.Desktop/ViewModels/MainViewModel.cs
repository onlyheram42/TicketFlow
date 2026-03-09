using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicketFlow.Desktop.Services;

namespace TicketFlow.Desktop.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ViewModelBase _currentViewModel;

        [ObservableProperty]
        private double _windowWidth = 450;

        [ObservableProperty]
        private double _windowHeight = 500;

        [ObservableProperty]
        private System.Windows.ResizeMode _windowResizeMode = System.Windows.ResizeMode.NoResize;


        [ObservableProperty]
        private bool _isLoggedIn;

        [ObservableProperty]
        private string _userNameDisplay = string.Empty;

        public MainViewModel()
        {
            SessionManager.Instance.SessionChanged += OnSessionChanged;

            // Start by showing the Login View
            _currentViewModel = new LoginViewModel(this);
            UpdateNavigationState();
        }

        private void OnSessionChanged()
        {
            UpdateNavigationState();
        }

        private void UpdateNavigationState()
        {
            IsLoggedIn = SessionManager.Instance.IsLoggedIn;

            if (IsLoggedIn)
            {
                UserNameDisplay = $"{SessionManager.Instance.FullName} ({SessionManager.Instance.Role})";
                WindowWidth = 1000;
                WindowHeight = 600;
                WindowResizeMode = System.Windows.ResizeMode.CanResize;
            }
            else
            {
                UserNameDisplay = string.Empty;
                WindowWidth = 450;
                WindowHeight = 500;
                WindowResizeMode = System.Windows.ResizeMode.NoResize;

                // Force return to login if session ends
                if (CurrentViewModel is not LoginViewModel)
                {
                    NavigateTo(new LoginViewModel(this));
                }
            }
        }

        [RelayCommand]
        public void Logout()
        {
            SessionManager.Instance.Logout();
        }

        public void NavigateTo(ViewModelBase viewModel)
        {
            CurrentViewModel = viewModel;
        }
    }
}
