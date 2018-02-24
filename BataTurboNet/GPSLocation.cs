namespace BataTurboNet
{
    internal class GPSLocation
    {
        public string version = "v0";
        public string messageType = "Location";
        public string deviceName;
        public int RadioID;
        public double Latitude;
        public double Longitude;
        public float Rssi;
    }
}