using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedometerSensor
{
    public class SpeedometerSensorTelemetry
    {
        public string id { get; set; }
        public string classifier { get; set; }
        public int ts { get; set; }
        public int id_mf { get; set; }
        public int can_pl_data_bytes { get; set; }
        public int td_ts_mf { get; set; }
        public int td_id_mf { get; set; }
        public int dlc { get; set; }
    }
}
