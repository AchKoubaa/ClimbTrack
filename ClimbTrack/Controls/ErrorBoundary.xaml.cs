using System;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace ClimbTrack.Controls
{
    public partial class ErrorBoundary : ContentView
    {
        public static readonly BindableProperty ChildContentProperty =
            BindableProperty.Create(nameof(ChildContent), typeof(View), typeof(ErrorBoundary),
                propertyChanged: OnChildContentChanged);

        public static readonly BindableProperty RetryCommandProperty =
            BindableProperty.Create(nameof(RetryCommand), typeof(ICommand), typeof(ErrorBoundary));

        public static readonly BindableProperty ErrorMessageProperty =
            BindableProperty.Create(nameof(ErrorMessage), typeof(string), typeof(ErrorBoundary));

        public static readonly BindableProperty IsErrorVisibleProperty =
            BindableProperty.Create(nameof(IsErrorVisible), typeof(bool), typeof(ErrorBoundary), false);

        public static readonly BindableProperty CanRetryProperty =
            BindableProperty.Create(nameof(CanRetry), typeof(bool), typeof(ErrorBoundary), true);

        public View ChildContent
        {
            get => (View)GetValue(ChildContentProperty);
            set => SetValue(ChildContentProperty, value);
        }

        public ICommand RetryCommand
        {
            get => (ICommand)GetValue(RetryCommandProperty);
            set => SetValue(RetryCommandProperty, value);
        }

        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public bool IsErrorVisible
        {
            get => (bool)GetValue(IsErrorVisibleProperty);
            set => SetValue(IsErrorVisibleProperty, value);
        }

        public bool CanRetry
        {
            get => (bool)GetValue(CanRetryProperty);
            set => SetValue(CanRetryProperty, value);
        }

        public ErrorBoundary()
        {
            InitializeComponent();
            BindingContext = this;
        }

        private static void OnChildContentChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (ErrorBoundary)bindable;
            control.ContentContainer.Content = newValue as View;
        }

        public void SetError(string message, bool canRetry = true)
        {
            ErrorMessage = message;
            CanRetry = canRetry;
            IsErrorVisible = true;
        }

        public void ClearError()
        {
            IsErrorVisible = false;
            ErrorMessage = null;
        }
    }
}