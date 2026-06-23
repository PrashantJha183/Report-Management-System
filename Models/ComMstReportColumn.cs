using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace Report.Models
{
    [System.ComponentModel.DataAnnotations.Schema.Table("com_mst_reportcolumn")]
    public class ComMstReportColumn
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int ReportColumnId { get; set; }
        public string DisplayColumnName { get; set; }
        public string TableColumnName { get; set; }
        public string Datatype { get; set; }
        public bool IsDefaultColumn { get; set; }
        public bool IsSqlParameter { get; set; }
        public int ReportId { get; set; }
        public string GroupType { get; set; }
        public int? GroupIndex { get; set; }
        public string SearchQuery { get; set; }
        public int? SrNo { get; set; }
        public bool IsFilterColumn { get; set; }
        public int? MainReportColumnId { get; set; }
    }

    public class ReportColumnGridItemViewModel
    {
        public int ReportColumnId { get; set; }
        public string DisplayColumnName { get; set; }
        public string TableColumnName { get; set; }
        public string Datatype { get; set; }
        public bool IsDefaultColumn { get; set; }
        public bool IsSqlParameter { get; set; }
        public int ReportId { get; set; }
        public string GroupType { get; set; }
        public int? GroupIndex { get; set; }
        public string SearchQuery { get; set; }
        public int? SrNo { get; set; }
        public bool IsFilterColumn { get; set; }
        public int? MainReportColumnId { get; set; }
    }

    public class ReportColumnChange
    {
        public int ReportColumnId { get; set; }
        public string Column { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsNew { get; set; }
    }

    public class SaveColumnChangesRequest
    {
        public string Mode { get; set; }
        public List<ReportColumnChange> Changes { get; set; }
    }

    public class SaveColumnChangesResult
    {
        public string Status { get; set; }
        public Dictionary<string, int> IdMappings { get; set; }
    }

    public class ReportColumnGridViewModel
    {
        public string SearchText { get; set; }
        public string SortColumn { get; set; }
        public string SortDirection { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        //public string FilterCode { get; set; }
        //public int FilterReportTypeId { get; set; }

        public int FilterReportId { get; set; }
        public string FilterLinkItemName { get; set; }

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

        public IList<ReportColumnGridItemViewModel> Items { get; set; }

        public ReportColumnGridViewModel()
        {
            SearchText = string.Empty;
            SortColumn = "ReportColumnId";
            SortDirection = "asc";
            PageNumber = 1;
            PageSize = 5;
            //FilterCode = string.Empty;
            //FilterReportTypeId = 0;
            //FilterReportTypeId = 0;
            Items = new List<ReportColumnGridItemViewModel>();
        }
    }
}