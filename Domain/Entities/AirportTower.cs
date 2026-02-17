using System;
using System.Collections.Generic;

namespace TMPP_Aeroport.Domain.Entities
{
    // High-level entity that might oversee operations.
    public class AirportTower : BaseEntity
    {
        public string Name { get; set; }
        public string LocationCode { get; set; }

        public AirportTower(string name, string locationCode)
        {
            Name = name;
            LocationCode = locationCode;
        }
    }
}
