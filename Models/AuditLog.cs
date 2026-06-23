using System;

namespace Report.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public string TableName { get; set; }
        public int RecordId { get; set; }
        public string OperationType { get; set; }
        public string OldValues { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string LinkItemName { get; set; }
    }

    public class AuditLogGridViewModel
    {
        public string TableName { get; set; }
        public int RecordId { get; set; }
        public System.Collections.Generic.List<AuditLog> Items { get; set; }
    }
}
