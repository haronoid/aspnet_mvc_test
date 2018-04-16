using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TuesPechkin;

namespace MvcWeb_PrintTest.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            var helper = new UrlHelper(ControllerContext.RequestContext);
            var indexUrl = helper.Action("Index", "Home", null, Request.Url.Scheme);

            var document = new HtmlToPdfDocument()
            {
                GlobalSettings =
                {
                    ProduceOutline = true,
                    DocumentTitle = "PDF Sample",
                    PaperSize = PaperKind.A4,
                    Margins =
                    {
                        All = 1.375,
                        Unit = Unit.Centimeters
                    }
                },
                Objects =
                {
                    new ObjectSettings() {
                        PageUrl = indexUrl,
                    },
                }
            };

            var converter = new StandardConverter(
                        new PdfToolset(
                            new WinAnyCPUEmbeddedDeployment(
                                new TempFolderDeployment())));

            var pdfData = converter.Convert(document);

            return File(pdfData, "application/pdf", "PdfSample.pdf");
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}