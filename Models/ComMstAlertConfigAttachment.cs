using System.Collections.Generic;
using System;

namespace Report.Models
{
    [System.ComponentModel.DataAnnotations.Schema.Table("com_mst_alertconfigattachment")]
    public class ComMstAlertConfigAttachment
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int AlertConfigAttachmentId { get; set; }
        public int AlertConfigId { get; set; }
        public int AttachmentFileTypeId { get; set; }
        public string EmailAttachmentUrl { get; set; }
    }

    public class AlertConfigAttachmentGridItemViewModel
    {
        public int AlertConfigAttachmentId { get; set; }
        public int AlertConfigId { get; set; }
        public int AttachmentFileTypeId { get; set; }
        public string EmailAttachmentUrl { get; set; }
    }

    public class AlertConfigAttachmentChange
    {
        public int AlertConfigAttachmentId { get; set; }
        public string Column { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsNew { get; set; }
    }

    public class SaveAlertConfigAttachmentChangesRequest
    {
        public string Mode { get; set; }
        public List<AlertConfigAttachmentChange> Changes { get; set; }
    }

    public class SaveAlertConfigAttachmentChangesResult
    {
        public string Status { get; set; }
        public Dictionary<string, int> IdMappings { get; set; }
    }

    public class AlertConfigAttachmentGridViewModel
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
            get { if (PageSize <= 0) return 0; return (int)Math.Ceiling((double)TotalRecords / PageSize); }
        }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public IList<AlertConfigAttachmentGridItemViewModel> Items { get; set; }

        public AlertConfigAttachmentGridViewModel()
        {
            SearchText = string.Empty;
            SortColumn = "AlertConfigAttachmentId";
            SortDirection = "asc";
            PageNumber = 1;
            PageSize = 5;
            Items = new List<AlertConfigAttachmentGridItemViewModel>();
        }
    }
}