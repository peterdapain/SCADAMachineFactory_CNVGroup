using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GiamSatNhaMay
{
    public class MachineModel
    {
        public int MachineID { get; set; }
        public string MachineName { get; set; }
        public string GroupName { get; set; }      // ← quan trọng, dùng để lọc
        public string TypeName { get; set; }
        public string Status { get; set; }
        public string ConnectionSTT { get; set; }

    }
    
   
    public class Machine
    {
        public int MachineID { get; set; }
        public string MachineName { get; set; }
    }

    public class MachineInfo
    {
        public string Manufacturer { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? LastMaintenance { get; set; }
        public string ContactPerson { get; set; }
        public string ErrorCodes { get; set; } 
    }



    public class BucketStatistic
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Label { get; set; }
        public double RunMinutes { get; set; }
        public double StopMinutes { get; set; }
        public double ErrorMinutes { get; set; }

        public double TotalMinutes => RunMinutes + StopMinutes + ErrorMinutes;
    }
    public class MachineOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool TagChecked { get; set; } // thêm dòng này
    }
    public class MachineLogEntry
    {
        public int MachineID { get; set; }
        public string Status { get; set; }  // "RUN"/"STOP"
        public DateTime LogTime { get; set; }
    }
    public class DailyStatistic
    {
        public DateTime Date { get; set; }
        public double RunMinutes { get; set; }
        public double StopMinutes { get; set; }
        public double ErrorMinutes { get; set; }

        public string DateLabel => Date.ToString("dd/MM/yyyy");
    }
    // class RowData
    public class RowData
    {
        public string Label { get; set; }
        public List<double> Values { get; set; }
        public List<string> Dates { get; set; }

        // Tổng cho các loại bình thường (RUN, STOP, ERROR)
        public double ValuesTotal { get; set; }
    }



}
