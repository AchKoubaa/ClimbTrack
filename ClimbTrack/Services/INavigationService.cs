namespace ClimbTrack.Services
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route, IDictionary<string, object> parameters = null);
        Task NavigateToMainPage();
        Task GoBackAsync();
        Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
        Task DisplayAlertAsync(string title, string message, string cancel);
        Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel);
    }
}
