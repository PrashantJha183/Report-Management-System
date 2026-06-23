using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Report.Models
{
    [System.ComponentModel.DataAnnotations.Schema.Table("com_mst_report")]
    public class ComMstReport
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int ReportId { get; set; }

        public int ReferenceLinkId { get; set; }

        public string Query { get; set; }

        public string WhereClause { get; set; }

        public string OrderBy { get; set; }

        public string DetailQuery { get; set; }

        public string DetailPrimaryKey { get; set; }

        public bool IsMasterDetail { get; set; }

        public bool IsStoreProcedure { get; set; }

        public string Code { get; set; }

        public int ReportTypeId { get; set; }

        public bool IsLocationFilter { get; set; }
    }

    public class ReportGridItemViewModel
    {
        public int ReportId { get; set; }

        public int ReferenceLinkId { get; set; }

        public string ReferenceLinkName { get; set; }

        public string Query { get; set; }

        public string WhereClause { get; set; }

        public string OrderBy { get; set; }

        public string DetailQuery { get; set; }

        public string DetailPrimaryKey { get; set; }

        public bool IsMasterDetail { get; set; }

        public bool IsStoreProcedure { get; set; }

        public string Code { get; set; }

        public int ReportTypeId { get; set; }

        public bool IsLocationFilter { get; set; }
    }


    public class ReportChange
    {
        public int ReportId { get; set; }
        public string Column { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsNew { get; set; }
    }



    public class SaveChangesRequest
    {
        public string Mode { get; set; } // "SAVE" or "CANCEL"
        public List<ReportChange> Changes { get; set; }
    }

    public class SaveChangesResult
    {
        public string Status { get; set; }
        public Dictionary<string, int> IdMappings { get; set; }
    }


    public class ReportGridViewModel
    {
        public string SearchText { get; set; }

        public string SortColumn { get; set; }

        public string SortDirection { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }
        public int FilterLinkItemId { get; set; }

      

        public int TotalPages
        {
            get
            {
                if (PageSize <= 0)
                {
                    return 0;
                }

                return (int)System.Math.Ceiling((double)TotalRecords / PageSize);
            }
        }

        public bool HasPreviousPage
        {
            get { return PageNumber > 1; }
        }

        public bool HasNextPage
        {
            get { return PageNumber < TotalPages; }
        }

        public System.Collections.Generic.IList<ReportGridItemViewModel> Items { get; set; }

        public ReportGridViewModel()
        {
            SearchText = string.Empty;
            SortColumn = "ReportId";
            SortDirection = "asc";
            PageNumber = 1;
            PageSize = 5;
            Items = new System.Collections.Generic.List<ReportGridItemViewModel>();
        }
    }
}