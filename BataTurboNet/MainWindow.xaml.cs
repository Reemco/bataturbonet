using NLog;
using NS.Enterprise.ClientAPI;
using NS.Enterprise.Objects.Devices;
using NS.Enterprise.Objects.Event_args;
using NS.Enterprise.Objects.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NS.Enterprise.Objects;
using NS.Enterprise.Objects.Map;
using System.Net.Http;
using Newtonsoft.Json;

namespace BataTurboNet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private Client m_client = new Client();
        private List<Device> devices = new List<Device>();
        private static HttpClient httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();


            if (Properties.Settings.Default.Simulation)
            {
                var gpsInfo = new GPSLocation();
                gpsInfo.devicename = "test";
                gpsInfo.Latitude = 52.4897266086191;
                gpsInfo.Longitude = 6.13765982910991;
                gpsInfo.Rssi = (float)-59.5864372253418;

                PostGpsLocation(gpsInfo);
            }
            else
            {
                ConnectToTurboNet();
            }
        }

        private async void PostGpsLocation(GPSLocation gps)
        {
            try
            {
                string json = JsonConvert.SerializeObject(gps, Formatting.Indented);
                logger.Debug(json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(Properties.Settings.Default.CdbUrl, content);

                logger.Info("PostObject :" + result.StatusCode);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex);
            }
        }

        #region TurboNet
        private void ConnectToTurboNet()
        {
            try
            {
                logger.Info("Connect to turbonet server");

                m_client.Disconnect();

                m_client.Connect(new NS.Network.NetworkConnectionParam(Properties.Settings.Default.TurboNetHost, Properties.Settings.Default.TurboNetPort), new UserInfo(Properties.Settings.Default.TurboNetUser, Properties.Settings.Default.TurboNetPassword), ClientInitFlags.Empty);
                if (m_client.IsStarted)
                {
                    logger.Info("Connected to turbonet server");
                }

                m_client.GetAllWorkflowCommands();

                devices = m_client.LoadRegisteredDevicesFromServer();

                m_client.BeaconSignal += M_client_BeaconSignal;
                m_client.DevicesChanged += DevicesChanged;
                m_client.DeviceLocationChanged += DeviceLocationChanged;
                m_client.DeviceStateChanged += M_client_DeviceStateChanged;
                m_client.TransmitReceiveChanged += M_client_TransmitReceiveChanged;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex);
            }
        }

        private void M_client_TransmitReceiveChanged(object sender, TransmitReceiveArgs e)
        {
            try
            {
                var device = devices.FirstOrDefault(r => r.ID == e.Info.ActiveDeviceID);

                StringBuilder build = new StringBuilder();
                build.Append("M_client_TransmitReceiveChanged");

                logger.Info(build.ToString());
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex);
            }
        }

        private void M_client_BeaconSignal(object sender, NS.Enterprise.Objects.Beacons.BeaconSignalEventArgs e)
        {
            try
            {
                foreach (var i in e.Infos)
                {
                    var device = devices.FirstOrDefault(r => r.ID == i.DeviceID);

                    StringBuilder build = new StringBuilder();
                    build.Append("M_client_BeaconSignal");
                    build.Append("device: " + device.Name + " ");
                    build.Append("batterylevel" + i.BatteryLevel + " ");

                    logger.Info(build.ToString());
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex);
            }
        }

        private void M_client_DeviceStateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            try
            {
                var device = devices.FirstOrDefault(r => r.ID == e.DeviceId);

                StringBuilder build = new StringBuilder();
                build.Append("M_client_DeviceStateChanged");
                build.Append("device: " + device.Name + " ");
                build.Append("state: " + e.State.ToString() + " ");

                logger.Info(build.ToString());
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex);
            }
        }

        private void DeviceLocationChanged(object sender, DeviceLocationChangedEventArgs e)
        {
            try
            {
                foreach (var i in e.GPSData)
                {
                    var device = devices.FirstOrDefault(r => r.ID == i.DeviceID);

                    StringBuilder build = new StringBuilder();
                    build.Append("DeviceLocationChanged");
                    build.Append("device: " + device.Name + " ");
                    build.Append("Altitude: " + i.Altitude + " ");
                    build.Append("Description: " + i.Description + " ");
                    build.Append("DeviceID: " + i.DeviceID + " ");
                    build.Append("Direction: " + i.Direction + " ");
                    build.Append("GpsSource: " + i.GpsSource + " ");
                    build.Append("InfoDate: " + i.InfoDate.ToString() + " ");
                    build.Append("InfoDateUtc: " + i.InfoDateUtc.ToString() + " ");
                    build.Append("Latitude: " + i.Latitude.ToString() + " ");
                    build.Append("Name: " + i.Name + " ");
                    build.Append("Radius: " + i.Radius.ToString() + " ");
                    build.Append("ReportId: " + i.ReportId.ToString() + " ");
                    build.Append("Rssi: " + i.Rssi.ToString() + " ");
                    build.Append("Speed: " + i.Speed.ToString() + " ");
                    build.Append("StopTime: " + i.StopTime.ToString() + " ");

                    logger.Info(build.ToString());

                    var gpsInfo = new GPSLocation();
                    gpsInfo.devicename = device.Name;
                    gpsInfo.Latitude = i.Latitude;
                    gpsInfo.Longitude = i.Longitude;
                    gpsInfo.Rssi = i.Rssi;

                    PostGpsLocation(gpsInfo);
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex);
            }
        }

        private void DevicesChanged(object sender, BindableCollectionEventArgs2<Device> e)
        {
            try
            {
                switch (e.Action)
                {
                    case NS.Shared.Common.ChangeAction.Add:
                        devices.Add(e.ChangedObject);
                        break;
                    case NS.Shared.Common.ChangeAction.Remove:
                        {
                            Device device = null;
                            foreach (Device item in devices)
                            {
                                if (e.ChangedObject.ID == item.ID)
                                {
                                    device = item;
                                    break;
                                }
                            }
                            if (device != null)
                                devices.Remove(device);
                        }
                        break;
                    case NS.Shared.Common.ChangeAction.ItemChanged:
                        {
                            Device device = null;
                            foreach (Device item in devices)
                            {
                                if (e.ChangedObject.ID == item.ID)
                                {
                                    device = item;
                                    break;
                                }
                            }
                            if (device != null)
                                device.Update(e.ChangedObject);
                        }
                        break;
                    case NS.Shared.Common.ChangeAction.MuchChanges:
                        devices.Clear();
                        devices = m_client.LoadRegisteredDevicesFromServer();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex);
            }
        }
        #endregion
    }
}
