using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Models
{
    public class GeneralResponse<T>
    {
        public T Data { get; set; }
        public bool Flag { get; set; } = true;
        public string Message { get; set; } = string.Empty;

        public static GeneralResponse<T> Success(T data, string message = "Operation completed successfully")
        {
            return new GeneralResponse<T>
            {
                Data = data,
                Flag = true,
                Message = message
            };
        }

        public static GeneralResponse<T> Failure(string message, T data = default)
        {
            return new GeneralResponse<T>
            {
                Data = data,
                Flag = false,
                Message = message
            };
        }
    }

}
