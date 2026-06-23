using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Report.Models
{
    [Table("com_mst_alertschedule")]
    public class ComMstAlertSchedule
    {
        [Key]
        public int AlertScheduleId { get; set; }
        public int AlertConfigId { get; set; }
        public string ToEmail { get; set; }
        public int? FrequencyHours { get; set; }
        public DateTime? NextScheduleDate { get; set; }
        public string BCCEmail { get; set; }
        public string CCEmail { get; set; }
    }

    public class AlertScheduleGridItemViewModel
    {
        public int AlertScheduleId { get; set; }
        public int AlertConfigId { get; set; }
        public string ToEmail { get; set; }
        public int? FrequencyHours { get; set; }
        public DateTime? NextScheduleDate { get; set; }
        public string BCCEmail { get; set; }
        public string CCEmail { get; set; }
    }

    public class AlertScheduleChange
    {
        public int AlertScheduleId { get; set; }
        public string Column { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsNew { get; set; }
    }

    public class SaveAlertScheduleChangesRequest
    {
        public string Mode { get; set; }
        public List<AlertScheduleChange> Changes { get; set; }
    }

    public class SaveAlertScheduleChangesResult
    {
        public string Status { get; set; }
        public Dictionary<string, int> IdMappings { get; set; }
    }

    public class AlertScheduleGridViewModel
    {
        public string SearchText { get; set; }
        public string SortColumn { get; set; }
        public string SortDirection { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int FilterAlertConfigId { get; set; }
        public string AlertName { get; set; }

        public int TotalPages
        {
            get
            {
                if (PageSize <= 0)
                    return 0;
                return (int)Math.Ceiling((double)TotalRecords / PageSize);
            }
        }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public IList<AlertScheduleGridItemViewModel> Items { get; set; }

        public AlertScheduleGridViewModel()
        {
            SearchText = string.Empty;
            SortColumn = "AlertScheduleId";
            SortDirection = "asc";
            PageNumber = 1;
            PageSize = 5;
            Items = new List<AlertScheduleGridItemViewModel>();
        }
    }
}