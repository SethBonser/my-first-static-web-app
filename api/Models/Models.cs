using System;

namespace Models
{
    internal class Models
    {
        internal class FlightInfoResponse
        {
            internal DateTime Date;
            internal string AircraftRegistration;
            internal string PointOfDestination;
            internal string PointOfOrigin;
            internal decimal TachoStart;
            internal decimal TachoEnd;
            internal decimal VdoStart;
            internal decimal VdoEnd;
            internal string PilotName;
            internal int Landings;
            internal int FuelStart;
            internal int FuelEnd;
            internal decimal FuelPurchased;
            internal decimal FuelAdded;
            internal int OilAdded;
            internal object LoggedAt;
            internal string Comments;
            internal string Landings_Other_Airports;
        }

        internal class ReportDownloadInfo
        {
            public Uri ReportDownloadUrl { get; set; }
        }
    }
}