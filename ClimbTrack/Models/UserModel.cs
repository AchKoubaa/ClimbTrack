using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Models
{
    public class UserModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        // Add climbing-specific properties
        public int ClimbsCompleted { get; set; }
        public string SkillLevel { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.Now;
    }
}
