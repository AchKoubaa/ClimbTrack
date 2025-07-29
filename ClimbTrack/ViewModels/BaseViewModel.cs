using ClimbTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.ViewModels
{
    public class BaseViewModel : BaseModel
    {
        private bool _isBusy;
        private string _title;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        protected bool SetBusy(bool value)
        {
            return SetProperty(ref _isBusy, value);
        }

        protected async Task ExecuteWithBusy(Func<Task> action)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await action();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
