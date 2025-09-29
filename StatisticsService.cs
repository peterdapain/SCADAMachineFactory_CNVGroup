using System;
using System.Collections.Generic;
using System.Linq;

namespace GiamSatNhaMay
{
    public class StatisticsService
    {
        private readonly DatabaseDAO dao;

        public StatisticsService(DatabaseDAO dao)
        {
            this.dao = dao ?? throw new ArgumentNullException(nameof(dao));
        }

        // ========================================================
        // 1. Thống kê cho ChartWindow (bucket động)
        // ========================================================
        public List<BucketStatistic> GetBucketedStatistics(int machineID, DateTime start, DateTime end)
        {
            if (end <= start) return new List<BucketStatistic>();

            // Lấy logs trong khoảng [start, end) — trạng thái trước start lấy riêng
            var logs = dao.GetMachineLogs(machineID, start, end)
                          .OrderBy(l => l.LogTime)
                          .ToList();

            var lastLogBeforeStart = dao.GetLastLogBeforeOrAt(machineID, start);
            string statusBeforeStart = lastLogBeforeStart?.Status ?? "STOP";

            List<BucketStatistic> buckets = new List<BucketStatistic>();
            TimeSpan totalSpan = end - start;
            DateTime now = DateTime.Now;

            // Helper để tính một bucket (dùng ở mọi chế độ)
            void ProcessBucket(DateTime bucketStart, DateTime bucketEnd, string label)
            {
                if (bucketEnd > now) bucketEnd = now;
                if (bucketEnd <= bucketStart) return;

                double runMinutes = 0, stopMinutes = 0, errorMinutes = 0;
                string currentStatus = statusBeforeStart;
                DateTime lastTime = bucketStart;

                var bucketLogs = logs.Where(l => l.LogTime >= bucketStart && l.LogTime < bucketEnd)
                                     .OrderBy(l => l.LogTime)
                                     .ToList();

                foreach (var log in bucketLogs)
                {
                    double minutes = (log.LogTime - lastTime).TotalMinutes;
                    if (minutes > 0)
                    {
                        if (currentStatus == "RUN") runMinutes += minutes;
                        else if (currentStatus == "STOP") stopMinutes += minutes;
                        else if (currentStatus == "ERROR") errorMinutes += minutes;
                    }

                    currentStatus = log.Status;
                    lastTime = log.LogTime;
                }

                double lastSegmentMinutes = (bucketEnd - lastTime).TotalMinutes;
                if (lastSegmentMinutes > 0)
                {
                    if (currentStatus == "RUN") runMinutes += lastSegmentMinutes;
                    else if (currentStatus == "STOP") stopMinutes += lastSegmentMinutes;
                    else if (currentStatus == "ERROR") errorMinutes += lastSegmentMinutes;
                }

                buckets.Add(new BucketStatistic
                {
                    Start = bucketStart,
                    End = bucketEnd,
                    Label = label,
                    RunMinutes = runMinutes,
                    StopMinutes = stopMinutes,
                    ErrorMinutes = errorMinutes
                });

                // chuyển trạng thái sang bucket tiếp theo
                statusBeforeStart = currentStatus;
            }

            if (totalSpan.TotalDays <= 1) // hourly buckets
            {
                DateTime cursor = start;
                while (cursor < end)
                {
                    DateTime bucketEnd = cursor.AddHours(1);
                    if (bucketEnd > end) bucketEnd = end;
                    ProcessBucket(cursor, bucketEnd, cursor.ToString("HH:mm"));
                    cursor = bucketEnd;
                }
            }
            else if (totalSpan.TotalDays <= 90) // daily buckets
            {
                DateTime cursor = start.Date;
                while (cursor < end)
                {
                    DateTime bucketEnd = cursor.AddDays(1);
                    if (bucketEnd > end) bucketEnd = end;
                    ProcessBucket(cursor, bucketEnd, cursor.ToString("dd/MM"));
                    cursor = bucketEnd;
                }
            }
            else if (totalSpan.TotalDays <= 730) // monthly (calendar months, clipped to start/end)
            {
                DateTime monthCursor = new DateTime(start.Year, start.Month, 1);
                while (monthCursor < end)
                {
                    DateTime bucketStart = monthCursor < start ? start : monthCursor;
                    DateTime bucketEnd = monthCursor.AddMonths(1);
                    if (bucketEnd > end) bucketEnd = end;
                    ProcessBucket(bucketStart, bucketEnd, bucketStart.ToString("MM/yyyy"));
                    monthCursor = monthCursor.AddMonths(1);
                }
            }
            else // yearly (calendar years)
            {
                DateTime yearCursor = new DateTime(start.Year, 1, 1);
                while (yearCursor < end)
                {
                    DateTime bucketStart = yearCursor < start ? start : yearCursor;
                    DateTime bucketEnd = yearCursor.AddYears(1);
                    if (bucketEnd > end) bucketEnd = end;
                    ProcessBucket(bucketStart, bucketEnd, bucketStart.ToString("yyyy"));
                    yearCursor = yearCursor.AddYears(1);
                }
            }

            return buckets;
        }

        // ========================================================
        // 2. Thống kê cho DataGridWindow (theo ngày)
        // ========================================================
        public List<DailyStatistic> CalculateDailyStatistics(int machineID, List<MachineLogEntry> logs, DateTime start, DateTime end)
        {
            var result = new List<DailyStatistic>();

            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                DateTime dayStart = date;
                DateTime dayEnd = date.AddDays(1);

                var dayLogs = logs.Where(l => l.LogTime >= dayStart && l.LogTime < dayEnd).OrderBy(l => l.LogTime).ToList();

                string currentStatus;
                var lastBefore = dao.GetLastLogBeforeOrAt(machineID, dayStart);
                if (lastBefore != null)
                    currentStatus = lastBefore.Status;
                else
                    currentStatus = dao.GetMachineCurrentStatusFromMachineTable(machineID) ?? "STOP";

                double run = 0, stop = 0, error = 0;
                DateTime cursor = dayStart;

                foreach (var log in dayLogs)
                {
                    double minutes = (log.LogTime - cursor).TotalMinutes;
                    if (currentStatus == "RUN") run += minutes;
                    else if (currentStatus == "STOP") stop += minutes;
                    else if (currentStatus == "ERROR") error += minutes;

                    currentStatus = log.Status;
                    cursor = log.LogTime;
                }

                double lastMinutes = (dayEnd - cursor).TotalMinutes;
                if (currentStatus == "RUN") run += lastMinutes;
                else if (currentStatus == "STOP") stop += lastMinutes;
                else if (currentStatus == "ERROR") error += lastMinutes;

                result.Add(new DailyStatistic
                {
                    Date = date,
                    RunMinutes = run,
                    StopMinutes = stop,
                    ErrorMinutes = error
                });
            }

            return result;
        }
    }
}
