using Report.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;

namespace Report.Controllers
{
    public class AuditTrailController : Controller
    {
        private readonly AuditLogService _auditLogService;

        public AuditTrailController()
        {
            _auditLogService = new AuditLogService();
        }

        [HttpGet]
        public async Task<ActionResult> GetAuditLogs(string tableName, int recordId)
        {
            try
            {
                var logs = await _auditLogService.GetAuditLogsAsync(tableName, recordId);
                var data = logs.Select(l => new {
                    l.AuditLogId,
                    l.TableName,
                    l.RecordId,
                    l.OperationType,
                    l.OldValues,
                    CreatedOn = l.CreatedOn.ToString("o"),
                    l.CreatedBy
                }).ToList();
                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> AuditLogs(string tableName, int recordId, string linkItemName = null, string alertName = null, string recordIdLabel = null, int page = 1, int pageSize = 5)
        {
            try
            {
                var totalRecords = await _auditLogService.GetAuditLogCountAsync(tableName, recordId);
                var logs = await _auditLogService.GetAuditLogsAsync(tableName, recordId, page, pageSize);
                ViewBag.Heading = "Change History";
                ViewBag.LinkItemName = linkItemName;
                ViewBag.AlertName = alertName;
                ViewBag.RecordId = recordId;
                ViewBag.RecordIdLabel = recordIdLabel ?? "Report ID";
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 1;
                return View(logs);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<Report.Models.AuditLog>());
            }
        }

        [HttpGet]
        public async Task<ActionResult> DeletedRecords(string tableName, int page = 1, int pageSize = 5)
        {
            try
            {
                var totalRecords = await _auditLogService.GetDeletedRecordsCountAsync(tableName);
                var logs = await _auditLogService.GetDeletedRecordsAsync(tableName, page, pageSize);
                ViewBag.Heading = "Deleted Records";
                ViewBag.PageNumber = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 1;

                if (tableName == "com_mst_report" && logs.Any())
                {
                    var linkItemIds = new HashSet<int>();
                    foreach (var log in logs)
                    {
                        if (!string.IsNullOrEmpty(log.OldValues))
                        {
                            try
                            {
                                var json = JObject.Parse(log.OldValues);
                                var refLinkId = json["ReferenceLinkId"]?.Value<int>();
                                if (refLinkId.HasValue)
                                    linkItemIds.Add(refLinkId.Value);
                            }
                            catch { }
                        }
                    }

                    var names = await _auditLogService.GetLinkItemNamesAsync(linkItemIds);
                    foreach (var log in logs)
                    {
                        if (!string.IsNullOrEmpty(log.OldValues))
                        {
                            try
                            {
                                var json = JObject.Parse(log.OldValues);
                                var refLinkId = json["ReferenceLinkId"]?.Value<int>();
                                if (refLinkId.HasValue && names.ContainsKey(refLinkId.Value))
                                    log.LinkItemName = names[refLinkId.Value];
                            }
                            catch { }
                        }
                    }
                }

                return View("AuditLogs", logs);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Heading = "Deleted Records";
                return View("AuditLogs", new List<Report.Models.AuditLog>());
            }
        }
    }
}
