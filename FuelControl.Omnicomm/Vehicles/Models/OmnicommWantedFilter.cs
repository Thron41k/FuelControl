using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuelControl.Omnicomm.Vehicles.Models
{
    public sealed class OmnicommWantedFilter
    {
        public IReadOnlyList<long> Groups { get; init; }
            = [];

        public IReadOnlyList<long> Objects { get; init; }
            = [];
    }
}
