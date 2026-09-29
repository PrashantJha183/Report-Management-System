using System;
using System.Collections.Generic;

namespace Report.Models
{
    // ── Grid row item (what the listing table shows) ──
    public class DynamicCodeGridItem
    {
        public int DynamicCodeId { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string Description { get; set; }
    }

    // ── Tracks a single cell change (same pattern as ReportChange) ──
    public class DynamicCodeChange
    {
        public int DynamicCodeId { get; set; }
        public string Column { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsNew { get; set; }
    }

    // ── Request from grid JS → SaveChanges action ──
    public class SaveDynamicCodeChangesRequest
    {
        public string Mode { get; set; }
        public List<DynamicCodeChange> Changes { get; set; }
    }

    // ── Result from SaveChanges → grid JS ──
    public class SaveDynamicCodeChangesResult
    {
        public string Status { get; set; }
        public Dictionary<string, int> IdMappings { get; set; }
    }

    // ── Grid view model (pagination + items) ──
    public class DynamicCodeGridViewModel
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
                if (PageSize <= 0) return 0;
                return (int)Math.Ceiling((double)TotalRecords / PageSize);
            }
        }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public IList<DynamicCodeGridItem> Items { get; set; }

        public DynamicCodeGridViewModel()
        {
            SearchText = string.Empty;
            SortColumn = "CustomCodeId";
            SortDirection = "asc";
            PageNumber = 1;
            PageSize = 5;
            Items = new List<DynamicCodeGridItem>();
        }
    }

    // ── IDE page view model (loads existing code or empty form) ──
    public class IDEViewModel
    {
        public int CodeID { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string Namespaces { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }
}