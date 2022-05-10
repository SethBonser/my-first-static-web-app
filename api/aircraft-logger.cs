using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;   
using System.IO;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;

namespace aircraft_logger
{
    public class Reporting
    {
        [FunctionName("new-aircraft-report")]
        public static async Task<IActionResult> newAircraftReportAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "reporting/new/{aircraftId}")] HttpRequest req, ILogger log, int aircraftId, IBinder binder)
        //[Blob("{aircraftId}/{sys.utcnow}.pdf", FileAccess.Write, Connection = "AzureWebJobsStorage")] Stream report)
        {
            var customerId = "1";

            if (customerId == null)
            {
                log.LogError("Not authorised to call this Function.");
                return new UnauthorizedResult();
            }

            List<Models.Models.FlightInfoResponse> flightList = new List<Models.Models.FlightInfoResponse>();
            try
            {
                await using SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlDatabase_ConnectionString"));

                connection.Open();
                const string query = @"Select fi.date, fi.pilot_name, fi.point_of_destination, fi.point_of_origin, fi.tacho_start, fi.tacho_end, fi.vdo_start, fi.vdo_end, fi.landings, ai.aircraft_registration, fi.fuel_start, fi.fuel_end, fi.fuel_purchased, fi.fuel_added, fi.comments, fi.oil_added, fi.landings_other_airports from dbo.flight_info as fi 
                        inner join aircraft_info as ai on ai.aircraft_id = fi.aircraft_id WHERE fi.customer_id = @customer_id and fi.aircraft_id = @aircraft_Id ORDER BY fi.date";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@customer_id", customerId);
                SqlParameter aircraftParameter = new SqlParameter
                {
                    ParameterName = "@aircraft_id",
                    Value = aircraftId
                };
                command.Parameters.Add(aircraftParameter);

                var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    Models.Models.FlightInfoResponse flightInfo = new Models.Models.FlightInfoResponse
                    {
                        Date = (DateTime)reader["date"],
                        AircraftRegistration = reader["aircraft_registration"]
                            .ToString(),
                        PointOfDestination = reader["point_of_destination"]
                            .ToString(),
                        PointOfOrigin = reader["point_of_origin"]
                            .ToString(),
                        TachoStart = Convert.IsDBNull(reader["tacho_start"])
                            ? 0
                            : Convert.ToDecimal(reader["tacho_start"]),
                        TachoEnd = Convert.IsDBNull(reader["tacho_end"])
                            ? 0
                            : Convert.ToDecimal(reader["tacho_end"]),
                        VdoStart = Convert.IsDBNull(reader["vdo_start"])
                            ? 0
                            : Convert.ToDecimal(reader["vdo_start"]),
                        VdoEnd = Convert.IsDBNull(reader["vdo_end"])
                            ? 0
                            : Convert.ToDecimal(reader["vdo_end"]),
                        PilotName = (string)reader["pilot_name"],
                        Landings = (int)reader["landings"],
                        FuelStart = (int)reader["fuel_start"],
                        FuelEnd = (int)reader["fuel_end"],
                        FuelPurchased = (decimal)reader["fuel_purchased"],
                        FuelAdded = (decimal)reader["fuel_added"],
                        OilAdded = (int)reader["oil_added"],
                        LoggedAt = null,
                        Comments = reader["comments"]
                            .ToString(),
                        Landings_Other_Airports = reader["landings_other_airports"]
                            .ToString()
                    };
                    flightList.Add(flightInfo);
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }

            // Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Environment.GetEnvironmentVariable("pdfKey"));
            // //Create a new PDF document.
            // PdfDocument outputFile = new PdfDocument();
            // outputFile.PageSettings.Orientation = PdfPageOrientation.Landscape;
            // //Add a page to the document.
            // PdfPage page = outputFile.Pages.Add();
            // PdfGraphics graphics = page.Graphics;
            // //Set the standard font.
            // PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 20);
            // //Draw the text.
            // graphics.DrawString($"Flight log for {flightList[0].AircraftRegistration}", font, PdfBrushes.Black, new PointF(0, 0));
            // // Create a PdfLightTable.
            // PdfLightTable pdfLightTable = new PdfLightTable
            // {
            //     Style =
            //     {
            //         ShowHeader = true
            //     }
            // };

            // DataTable table = new DataTable();
            // table.Columns.Add("Date");
            // table.Columns.Add("Aircraft Registration");
            // table.Columns.Add("Name");
            // table.Columns.Add("Origin");
            // table.Columns.Add("Destination");
            // table.Columns.Add("Tacho Start");
            // table.Columns.Add("Tacho End");
            // table.Columns.Add("VDO Start");
            // table.Columns.Add("VDO End");
            // table.Columns.Add("Number of landings");
            // table.Columns.Add("Landings at other airports");
            // table.Columns.Add("Fuel at start");
            // table.Columns.Add("Fuel at end");
            // table.Columns.Add("Fuel Purchased at pilot's expense");
            // table.Columns.Add("Fuel added");
            // table.Columns.Add("Oil added");
            // table.Columns.Add("Comments");

            // foreach (var flight in flightList)
            // {
            //     table.Rows.Add(flight.Date.ToString("dd/MM/yyyy"), flight.AircraftRegistration, flight.PilotName, flight.PointOfOrigin, flight.PointOfDestination, flight.TachoStart, flight.TachoEnd, flight.VdoStart, flight.VdoEnd, flight.Landings, flight.Landings_Other_Airports, flight.FuelStart, flight.FuelEnd, flight.FuelPurchased, flight.FuelAdded, flight.OilAdded, flight.Comments);
            // }

            // pdfLightTable.DataSource = table;
            // pdfLightTable.AllowRowBreakAcrossPages = true;

            // pdfLightTable.ApplyBuiltinStyle(PdfLightTableBuiltinStyle.GridTable4Accent1);
            // PdfLightTableLayoutFormat layoutFormat = new PdfLightTableLayoutFormat
            // {
            //     Break = PdfLayoutBreakType.FitPage,
            //     Layout = PdfLayoutType.Paginate
            // };

            // pdfLightTable.Draw(page, new PointF(0, 100), layoutFormat);



            var outboundBlob = new BlobAttribute($"reports/{customerId}/{flightList[0].AircraftRegistration}.pdf", FileAccess.Write);
            await using var writer = binder.Bind<Stream>(outboundBlob);

            //outputFile.Save(writer);

            var sasUrl = GetSasForBlob($"{flightList[0].AircraftRegistration}.pdf", $"reports/{customerId}", DateTime.UtcNow.AddMinutes(2));

            var reportDownloadInfo = new Models.Models.ReportDownloadInfo
            {
                ReportDownloadUrl = sasUrl
            };

            return new OkObjectResult(reportDownloadInfo);
        }

        private static Uri GetSasForBlob(string blobname, string containerName, DateTime expiry, BlobAccountSasPermissions permissions = BlobAccountSasPermissions.Read)
        {
            var offset = TimeSpan.FromMinutes(10);

            var credential = new StorageSharedKeyCredential(Environment.GetEnvironmentVariable("storage_account_name"), Environment.GetEnvironmentVariable("storage_account_key"));
            var sas = new BlobSasBuilder
            {
                BlobName = blobname,
                BlobContainerName = containerName,
                StartsOn = DateTime.UtcNow.Subtract(offset),
                ExpiresOn = expiry.Add(offset)
            };
            sas.SetPermissions(permissions);

            UriBuilder sasUri = new UriBuilder($"{Environment.GetEnvironmentVariable("storage_blob_service_endpoint")}{containerName}/{blobname}");
            sasUri.Query = sas.ToSasQueryParameters(credential).ToString();

            return sasUri.Uri;
        }
    }
}
