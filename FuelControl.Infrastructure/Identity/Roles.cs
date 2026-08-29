using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelControl.Infrastructure.Identity
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Dispatcher = "Dispatcher";
        public const string Moderator = "Moderator";

        public static readonly string[] All = [Admin, Dispatcher,Moderator];
    }
}
