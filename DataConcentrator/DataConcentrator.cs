using PLCSimulator;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DataConcentrator
{
    public class DataConcentrator
    {
        public event EventHandler<ActivatedAlarm> AlarmOccurred;
        public event EventHandler ValueChanged;
        public static PLCSimulatorManager PLC;
        public List<ReportObject> reportList {  get; set; }

        public class ReportObject
        {
            public Tag tag { get; set; }
            public DateTime timestamp { get; set; }


            public override string ToString()
            {
                return $"{tag} : {timestamp}";
            }
        }
        public DataConcentrator()
        {
            PLC = new PLCSimulatorManager();
            reportList = new List<ReportObject>();
        }

        
        public void ReadTagValue(Tag tag)
        {
            tag.Value = Convert.ToDouble(PLC.GetValue(tag.IOAddress));


            using (var db = new ContextClass())
            {
                var selectedTag = db.Tags
                    .Include("Alarms")
                    .FirstOrDefault(t => t.Id == tag.Id);

                if (selectedTag != null)
                {
                    selectedTag.Value = tag.Value;

                    db.Tags.AddOrUpdate(selectedTag);
                    db.SaveChanges();

                    ValueChanged?.Invoke(this, EventArgs.Empty);


                    if (selectedTag.Alarms != null)
                    {
                        foreach (var alarm in selectedTag.Alarms)
                        {
                            if ((alarm.Type == AlarmType.Above && selectedTag.Value > alarm.Limit) ||
                                (alarm.Type == AlarmType.Below && selectedTag.Value < alarm.Limit))
                            {
                                var activated = new ActivatedAlarm
                                {
                                    AlarmId = alarm.Id,
                                    TagName = selectedTag.Name,
                                    Timestamp = DateTime.Now,
                                    Type = Convert.ToString(alarm.Type),
                                    Limit = alarm.Limit,
                                    Value = selectedTag.Value,
                                    Message = alarm.Message,
                                    Active = true
                                };

                                bool alreadyActive = db.ActivatedAlarms
                                    .Any(a => a.AlarmId == alarm.Id && a.TagName == selectedTag.Name && a.Active);

                                if (!alreadyActive)
                                {
                                    db.ActivatedAlarms.Add(activated);
                                    db.SaveChanges();
                                    AlarmOccurred?.Invoke(this, activated);
                                }
                            }
                        }
                    }

                    try
                    {

                        var lowEl = (JsonElement)selectedTag.ExtraProperties[TagProperty.lowlimit];
                        var highEl = (JsonElement)selectedTag.ExtraProperties[TagProperty.highlimit];

                        double low = lowEl.ValueKind == JsonValueKind.Number ? lowEl.GetDouble()
                                                                             : double.Parse(lowEl.GetString());
                        double high = highEl.ValueKind == JsonValueKind.Number ? highEl.GetDouble()
                                                                               : double.Parse(highEl.GetString());

                        double reportLow = (low + high) / 2 - 5;
                        double reportHigh = (low + high) / 2 + 5;


                        if (selectedTag.Value > reportLow && selectedTag.Value < reportHigh)
                        {

                            ReportObject ro = new ReportObject();
                            ro.timestamp = DateTime.Now;
                            ro.tag = selectedTag;
                            reportList.Add(ro);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Stopped here" + ex);
                    }
                }
            }
        }

        public void ForceTagValue(Tag tag, object newValue)
        {
            PLC.SetValue(tag.IOAddress, Convert.ToDouble(newValue));
            tag.Value = Convert.ToDouble(newValue);

            using (var db = new ContextClass())
            {
                db.Tags.AddOrUpdate(tag);
                db.SaveChanges();
            }

            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void addTag(Tag tag)
        {
            using (var db = new ContextClass())
            {
                db.Tags.AddOrUpdate(tag);
                db.SaveChanges();
            }

            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}