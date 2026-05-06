using System;

namespace ZansiHustle.Application.Common.Geo
{
    /// <summary>
    /// Pure geographic helpers shared by the application + infrastructure
    /// layers. Lives in the Application project so Infrastructure can
    /// reference it without inverting the dependency direction.
    /// </summary>
    public static class Haversine
    {
        /// <summary>
        /// Great-circle distance in kilometres between two lat/lng pairs
        /// using the Earth's mean radius (6371 km). Pure function: same
        /// inputs → same output, no allocations.
        /// </summary>
        public static double DistanceKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371.0;

            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLng = (lng2 - lng1) * Math.PI / 180.0;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(lat1 * Math.PI / 180.0)
                  * Math.Cos(lat2 * Math.PI / 180.0)
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
