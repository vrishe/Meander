using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace Meander;

public sealed partial class StartUpViewModel
{
    private readonly IShellNavigation _navigation;

    public StartUpViewModel(IShellNavigation navigation)
    {
        _navigation = navigation;
    }

    [RelayCommand]
    private Task DoNew() => _navigation.GoToAsync(Routes.ProjectSetupUrl);
}
