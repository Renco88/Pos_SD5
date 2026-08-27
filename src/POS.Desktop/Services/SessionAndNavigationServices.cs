using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.DTOs;
using POS.Domain.Enums;

namespace POS.Desktop.Services;

public interface IAuthSession : INotifyPropertyChanged
{
    UserDto? CurrentUser { get; }
    string? Token { get; }
    bool IsAuthenticated { get; }
    bool IsEmployer { get; }
    bool IsWorker { get; }
    CashSessionDto? CurrentCashSession { get; set; }

    void SetSession(LoginResponse response);
    void UpdateUser(UserDto user);
    void Clear();
    bool HasPermission(string permission);
}

public class AuthSession : IAuthSession
{
    private UserDto? _currentUser;
    private string? _token;
    private CashSessionDto? _currentCashSession;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserDto? CurrentUser
    {
        get => _currentUser;
        private set { _currentUser = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsAuthenticated)); OnPropertyChanged(nameof(IsEmployer)); OnPropertyChanged(nameof(IsWorker)); }
    }

    public string? Token
    {
        get => _token;
        private set { _token = value; OnPropertyChanged(); }
    }

    public CashSessionDto? CurrentCashSession
    {
        get => _currentCashSession;
        set { _currentCashSession = value; OnPropertyChanged(); }
    }

    public bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_token);
    public bool IsEmployer => _currentUser?.Role == Roles.Employer;
    public bool IsWorker => _currentUser?.Role == Roles.Worker;

    public void SetSession(LoginResponse response)
    {
        Token = response.Token;
        CurrentUser = response.User;
    }

    public void UpdateUser(UserDto user)
    {
        CurrentUser = user;
    }

    public void Clear()
    {
        Token = null;
        CurrentUser = null;
        CurrentCashSession = null;
    }

    public bool HasPermission(string permission)
    {
        if (IsEmployer) return true;
        return _currentUser?.Permissions.Contains(permission) == true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public interface INavigationService : INotifyPropertyChanged
{
    object? CurrentViewModel { get; }
    string CurrentViewName { get; }
    void NavigateTo<TViewModel>() where TViewModel : class;
    void NavigateTo(Type viewModelType);
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private object? _currentViewModel;
    private string _currentViewName = "Login";

    public event PropertyChangedEventHandler? PropertyChanged;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set { _currentViewModel = value; OnPropertyChanged(); }
    }

    public string CurrentViewName
    {
        get => _currentViewName;
        private set { _currentViewName = value; OnPropertyChanged(); }
    }

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        NavigateTo(typeof(TViewModel));
    }

    public void NavigateTo(Type viewModelType)
    {
        try
        {
            var vm = _serviceProvider.GetService(viewModelType);
            if (vm != null)
            {
                CurrentViewModel = vm;
                CurrentViewName = viewModelType.Name.Replace("ViewModel", "");
            }
            else
            {
                MessageBox.Show(
                    $"Could not load page '{viewModelType.Name}'. The required component was not registered.",
                    "Navigation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NavigationService] NavigateTo {viewModelType.Name} failed: {ex}");
            MessageBox.Show(
                $"Could not open this page due to an error.\n\n" +
                $"Page: {viewModelType.Name.Replace("ViewModel", "")}\n" +
                $"Error: {ex.Message}\n\n" +
                $"Type: {ex.GetType().Name}",
                "⚠️ Navigation Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
