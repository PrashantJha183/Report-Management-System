
using System.Collections.Generic;
using System;

namespace Report.Models
{
    [System.ComponentModel.DataAnnotations.Schema.Table("com_mst_companyconfig")]
    public class ComMstCompanyConfig
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int CompanyConfigId { get; set; }
        public int CompanyId { get; set; }
        public string Code { get; set; }
        public string ConfigValue { get; set; }
        public string Description { get; set; }
    }

    public class CompanyConfigGridItemViewModel
    {
        public int CompanyConfigId { get; set; }
        public int CompanyId { get; set; }
        public string Code { get; set; }
        public string ConfigValue { get; set; }
        public string Description { get; set; }
    }

    public class CompanyConfigChange
    {
        public int CompanyConfigId { get; set; }
        public string Column { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsNew { get; set; }
    }

    public class SaveCompanyConfigChangesRequest
    {
        public string Mode { get; set; }
        public List<CompanyConfigChange> Changes { get; set; }
    }

    public class SaveCompanyConfigChangesResult
    {
        public string Status { get; set; }
        public Dictionary<string, int> IdMappings { get; set; }
    }

    public class CompanyConfigGridViewModel
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

        public IList<CompanyConfigGridItemViewModel> Items { get; set; }

        public CompanyConfigGridViewModel()
        {
            SearchText = string.Empty;
            SortColumn = "CompanyConfigId";
            SortDirection = "asc";
            PageNumber = 1;
            PageSize = 5;
            Items = new List<CompanyConfigGridItemViewModel>();
        }
    }
}
