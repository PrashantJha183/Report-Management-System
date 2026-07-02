using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Report.Models
{
    /// <summary>
    /// Base class for all grid view models.
    /// Provides common pagination, sorting, and search properties.
    /// </summary>
    public class GridViewModelBase
    {
        public string SearchText { get; set; }
        public string SortColumn { get; set; }
        public string SortDirection { get; set; } = "asc";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalRecords { get; set; }
        public int TotalPages
        {
            get
            {
                if (PageSize <= 0) return 1;
                return (int)Math.Ceiling((double)TotalRecords / PageSize);
            }
        }
    }

    /// <summary>
    /// Generic grid view model with typed items.
    /// </summary>
    public class GridViewModel<TItem> : GridViewModelBase
    {
        public List<TItem> Items { get; set; } = new List<TItem>();
    }
}
