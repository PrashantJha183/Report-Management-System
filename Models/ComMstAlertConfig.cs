
using System.Collections.Generic;
using System;

namespace Report.Models
{
    [System.ComponentModel.DataAnnotations.Schema.Table("com_mst_alertconfig")]
    public class ComMstAlertConfig
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int AlertConfigId { get; set; }
        public string AlertName { get; set; }
        public int ReportId { get; set; }
        public bool IsEmailNotify { get; set; }
        public string EmailSubject { get; set; }
        public string EmailContentHeader { get; set; }
        public string EmailContentUrl { get; set; }
        public int AttachmentFileTypeId { get; set; }
        public string EmailAttachmentUrl { get; set; }
        public int StatusId { get; set; }
        public int AlertTypeId { get; set; }
        public bool IsMultiAlert { get; set; }
        public string MultiAlertQuery { get; set; }
        public string EmailContentFooter { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class AlertConfigGridItemViewModel
    {
        public int AlertConfigId { get; set; }
        public string AlertName { get; set; }
        public int ReportId { get; set; }
        public bool IsEmailNotify { get; set; }
        public string EmailSubject { get; set; }
        public string EmailContentHeader { get; set; }
        public string EmailContentUrl { get; set; }
        public int AttachmentFileTypeId { get; set; }
        public string EmailAttachmentUrl { get; set; }
        public int StatusId { get; set; }
        public int AlertTypeId { get; set; }
        public bool IsMultiAlert { get; set; }
        public string MultiAlertQuery { get; set; }
        public string EmailContentFooter { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class AlertConfigChange
    {
        public int AlertConfigId { get; set; }
        public string Column { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsNew { get; set; }
    }

    public class SaveAlertConfigChangesRequest
    {
        public string Mode { get; set; }
        public List<AlertConfigChange> Changes { get; set; }
    }

    public class SaveAlertConfigChangesResult
    {
        public string Status { get; set; }
        public Dictionary<string, int> IdMappings { get; set; }
    }

    public class AlertConfigGridViewModel
    {
        public string SearchText { get; set; }
        public string SortColumn { get; set; }
        public string SortDirection { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }

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

        public IList<AlertConfigGridItemViewModel> Items { get; set; }

        public AlertConfigGridViewModel()
        {
            SearchText = string.Empty;
            SortColumn = "AlertConfigId";
            SortDirection = "asc";
            PageNumber = 1;
            PageSize = 5;
            Items = new List<AlertConfigGridItemViewModel>();
        }
    }
}