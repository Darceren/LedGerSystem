using LedGerSystem.Models;

using LedGerSystem.Services;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;



namespace LedGerSystem.Controllers;



[Authorize]

public class ReportController(

    IBalanceService balanceService,

    IReportService reportService,

    IExcelExportService excelExportService) : Controller

{

    [HttpGet]

    public async Task<IActionResult> FinalAccount(DateTime? date = null, string? export = null)

    {

        var asOfDate = date?.Date ?? DateTime.Today;

        var model = await balanceService.GetFinalAccountAsync(asOfDate);



        if (export == "excel")

        {

            var bytes = excelExportService.ExportFinalAccount(model);

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                $"FinalAccount_{asOfDate:yyyy-MM-dd}.xlsx");

        }



        return View(model);

    }



    [HttpGet]

    public async Task<IActionResult> DailyRmbPayout(DateTime? date = null, string? export = null)

    {

        var reportDate = date?.Date ?? DateTime.Today;

        var model = await reportService.GetDailyRmbPayoutAsync(reportDate);



        if (export == "excel")

        {

            var bytes = excelExportService.ExportDailyRmbPayout(model);

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                $"DailyRmbPayout_{reportDate:yyyy-MM-dd}.xlsx");

        }



        return View(model);

    }



    [HttpGet]

    public async Task<IActionResult> DailyBdtCollection(DateTime? date = null, string? export = null)

    {

        var reportDate = date?.Date ?? DateTime.Today;

        var model = await reportService.GetDailyBdtCollectionAsync(reportDate);



        if (export == "excel")

        {

            var bytes = excelExportService.ExportDailyBdtCollection(model);

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                $"DailyBdtCollection_{reportDate:yyyy-MM-dd}.xlsx");

        }



        return View(model);

    }



    [HttpGet]

    public async Task<IActionResult> ProfitAnalysis(DateTime? from = null, DateTime? to = null, string? export = null)

    {

        var dateFrom = from?.Date ?? DateTime.Today.AddDays(-30);

        var dateTo = to?.Date ?? DateTime.Today;

        if (dateTo < dateFrom)

        {

            (dateFrom, dateTo) = (dateTo, dateFrom);

        }



        var model = await reportService.GetProfitAnalysisAsync(dateFrom, dateTo);



        if (export == "excel")

        {

            var bytes = excelExportService.ExportProfitAnalysis(model);

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                $"ProfitAnalysis_{dateFrom:yyyy-MM-dd}_{dateTo:yyyy-MM-dd}.xlsx");

        }



        return View(model);

    }

}


