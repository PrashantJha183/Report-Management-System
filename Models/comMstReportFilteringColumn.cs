using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Report.Models
{
    [System.ComponentModel.DataAnnotations.Schema.Table("com_mst_reportfilteringcolumn")]
    public class ComMstReportFilteringColumn
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int ReportFilterColumnId { get; set; }
        public int? ReportColumnId { get; set; }
        public int? ReportId { get; set; }
        public string DataType { get; set; }
        public string Operator { get; set; }
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public string Condn { get; set; }
        public bool? IsDisable { get; set; }
    }

    public class ReportFilteringColumnGridItemViewModel
    {
        public int ReportFilterColumnId { get; set; }
        public int? ReportColumnId { get; set; }
        public int? ReportId { get; set; }
        public string DataType { get; set; }
        public string Operator { get; set; }
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public string Condn { get; set; }
        public bool? IsDisable { get; set; }
    }

    public class ReportFilteringColumnChange
    {
        public int ReportFilterColumnId { get; set; }
        public string Column { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsNew { get; set; }
    }

    public class SaveFilteringColumnChangesRequest
    {
        public string Mode { get; set; }
        public List<ReportFilteringColumnChange> Changes { get; set; }
    }

    public class SaveFilteringColumnChangesResult
    {
        public string Status { get; set; }
        public Dictionary<string, int> IdMappings { get; set; }
    }

    public class ReportFilteringColumnGridViewModel
    {
        public string SearchText { get; set; }
        public string SortColumn { get; set; }
        public string SortDirection { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int FilterReportId { get; set; }
        public string FilterLinkItemName { get; set; }
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

        public IList<ReportFilteringColumnGridItemViewModel> Items { get; set; }

        public ReportFilteringColumnGridViewModel()
        {
            SearchText = string.Empty;
            SortColumn = "ReportFilterColumnId";
            SortDirection = "asc";
            PageNumber = 1;
            PageSize = 5;
            FilterReportId = 0;
            Items = new List<ReportFilteringColumnGridItemViewModel>();
        }
    }
}
